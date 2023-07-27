# 版本变化

- [[0.0.2v]](#002v---2023727)
- [[0.0.1v]](#001v---2023725)

## [0.0.2v] - 2023/7/27

- Features
    - 扫码登录，Cookie以JSON储存本地
    - 搜索结果翻页（效果不太理想需改进）
- Changed
    - 搜索结果返回全部，20个为一页
    - 将专辑封面限制为100pi x 100pi大小
- Bugs
    - 滑动过快时LitJson报错`MissingMethodException: Default constructor not found for type System.String`，下版本修复。
    - Build时会出现`System.Windows.Forms.dll assembly is referenced by user code, but is not supported on StandaloneWindows64 platform. Various failures might follow.`错误，实际为QRCoder.Unity以.Net Standard 3.5构建，引用于.Net framework所导致的，不影响构建。

## [0.0.1v] - 2023/7/25

- Features
    - 搜索功能（只返回20条）
    - 专辑封面（返回的是原始大小）
    - 播放
- Dependencies
    - [LitJson](https://github.com/LitJSON/litjson)
    - [QRCode.Unity](https://github.com/codebude/QRCoder.Unity)
    - [UniRx](https://github.com/neuecc/UniRx)
    - [AsyncAwaitUtil](https://github.com/modesttree/Unity3dAsyncAwaitUtil)
