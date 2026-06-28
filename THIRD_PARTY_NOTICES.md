# 第三方开源鸣谢

PixelBar 在部分功能中参考或移植了以下开源项目。各项目均保留其原有许可证；此处列出仓库链接与在本项目中的用途，便于溯源与合规。

---

## jixunmoe-go/qrc

- **仓库**: [https://github.com/jixunmoe-go/qrc](https://github.com/jixunmoe-go/qrc)
- **许可证**: [MIT](https://github.com/jixunmoe-go/qrc/blob/main/LICENSE)
- **用途**: QQ 音乐本地缓存 `*_qm.qrc` 的解密算法（QMC 异或 + 自定义 DES + zlib），已移植为 C# 实现
- **本仓库位置**: `src/PixelBar.App/Services/Lyrics/QmQrcDecoder.cs`、`QmQrcDes.cs`；`tools/QrcDecryptTest` 复用同一实现做命令行验证

---

## MIT License 摘要

上述 MIT 项目允许在保留版权与许可声明的前提下使用、修改与分发。完整条款见各仓库 `LICENSE` 文件。
