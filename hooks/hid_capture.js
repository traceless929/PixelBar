function bytesToHex(ptr, len) {
    if (ptr.isNull() || len <= 0) return '';
    var bytes = ptr.readByteArray(Math.min(len, 128));
    var view = new Uint8Array(bytes);
    var out = '';
    for (var i = 0; i < view.length; i++) {
        out += ('0' + view[i].toString(16)).slice(-2);
    }
    return out;
}

function isPixelPacket(ptr, len) {
    if (ptr.isNull() || len < 4) return false;
    return ptr.readU8() === 0x2e &&
        ptr.add(1).readU8() === 0xaa &&
        ptr.add(2).readU8() === 0xec;
}

function attachExport(moduleName, exportName, tag, onEnterBuilder) {
    var mod = Process.findModuleByName(moduleName);
    if (!mod) return false;

    var addr = mod.findExportByName(exportName);
    if (!addr) return false;

    send('[+] ' + tag + ' ' + moduleName + '!' + exportName + ' @ ' + addr);
    Interceptor.attach(addr, onEnterBuilder(tag, exportName));
    return true;
}

function hookWriteLike(tag, exportName, dataIndex, lenIndex, filterMode) {
    attachExport('kernel32.dll', exportName, tag, function () {
        return {
            onEnter: function (args) {
                var data = args[dataIndex];
                var len = args[lenIndex].toInt32();
                if (filterMode === 'packet' && !(len === 64 && isPixelPacket(data, len))) return;
                if (filterMode === 'packet' && len === 65 && isPixelPacket(data.add(1), 64)) {
                    send('[' + tag + ' ' + exportName + '] len=65 reportId | ' + bytesToHex(data.add(1), 64));
                    return;
                }
                send('[' + tag + ' ' + exportName + '] len=' + len + ' | ' + bytesToHex(data, len));
            }
        };
    });
}

function hookHidWrite(moduleName) {
    attachExport(moduleName, 'hid_write', 'HIDAPI', function () {
        return {
            onEnter: function (args) {
                var data = args[1];
                var len = args[2].toInt32();
                send('[HIDAPI hid_write] len=' + len + ' | ' + bytesToHex(data, len));
            }
        };
    });
}

var filterMode = '{{FILTER_MODE}}';

hookWriteLike('KERNEL32', 'WriteFile', 1, 2, filterMode);
hookWriteLike('KERNEL32', 'ReadFile', 1, 2, filterMode);

['hid.dll', 'hidapi.dll', 'hid.cp39-win_amd64.pyd'].forEach(function (name) {
    hookHidWrite(name);
    attachExport(name, 'hid_send_feature_report', 'HIDAPI', function () {
        return {
            onEnter: function (args) {
                var data = args[1];
                var len = args[2].toInt32();
                send('[HIDAPI hid_send_feature_report] len=' + len + ' | ' + bytesToHex(data, len));
            }
        };
    });
});

send('Hooks ready. filter=' + filterMode + '. Operate TempoHub to capture HID traffic.');
