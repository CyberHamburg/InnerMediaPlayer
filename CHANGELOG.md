# 版本变化

- [[0.6.2v]](#062v---2024331)
- [[0.6.1v]](#061v---202435)
- [[0.5.6v]](#056v---2024223)
- [[0.5.5v]](#055v---202426)
- [[0.5.4v]](#054v---202416)
- [[0.5.3v]](#053v---20231223)
- [[0.5.2v]](#052v---20231214)
- [[0.4.3v]](#043v---2023921)
- [[0.0.2v]](#002v---2023727)
- [[0.0.1v]](#001v---2023725)

## [0.6.2v] - 2024/3/31

- Features
  - 添加播放歌曲时未能成功播放的提示语
- Changed
  - 修改展示与播放歌曲时会员权限的判断

## [0.6.1v] - 2024/3/5

- Features
  - 现在可以在关闭时自动保存已添加的歌曲，在下次打开时可选择读取上一次的列表
- Changed
  - 删除场景中部分错误引用的游戏物体
  - 修复拖拽查看歌词时高亮歌词不正确回滚的问题
  - 修复有时间轴歌词与无时间轴歌词之间，错误设置字段导致的的切换方式不正确问题
  - 解决解析歌词时间轴解析格式不匹配的问题

## [0.5.6v] - 2024/2/23

- Features
  - 现在鼠标滚动歌词时，会有短暂停留再回复初始位置
- Changed
  - 修复暂停时点击播放歌曲后导致进度条冻结的bug
  - 修复提示语在某些时刻不正确显示的问题
  - 修复部分歌词解析错误的问题，重构了方法

## [0.5.5v] - 2024/2/6

- Features
  - 添加对无时间轴纯文本歌词的提示
  - 歌词大小对多分辨率自动适配
- Changed
  - 修复部分歌词解析错误的问题
  - 修复切换歌曲后高亮歌词未正确显示的问题
  - 修复切换歌曲后UI不正确显示的问题

## [0.5.4v] - 2024/1/6

- Features
  - 添加互动时提示UI
- Changed
  - 调节画质并限制帧数为60以便得到更好体验
  - 取消部分UI射线投射和可遮罩选项勾选
  - 修复内存泄漏问题，但内存页分配问题仍然存在

## [0.5.3v] - 2023/12/23

- Changed
  - 在下载歌曲之前验证是否已经添加过
  - 减少部分GC的产生
- Bugs
  - 添加歌曲后删除并没有释放全部内存，存在内存泄漏问题

## [0.5.2v] - 2023/12/14

- Features
  - 歌曲进度条及进度跳转
  - 歌词颜色与背景颜色自动区分
- Changed
  - 当歌词背景颜色过深或过浅时，自动调整歌词颜色避免看不清歌词
  - 增加了歌曲进度条以及时长显示，可通过点击或拖拽调整时间轴
  - 出于测试目的添加了下载歌曲到桌面的方法（需在代码中开启，默认关闭）
  - 修复调整列表歌曲顺序时找不到引用的bug
- Bugs
  - 歌词UI高度长度未适配，只能通过提升分辨率解决
  - 不包含时间轴歌词未逐行适配分辨率
  - 网络条件差或过大的歌曲无法及时响应播放

## [0.4.3v] - 2023/9/21

- Features
    - 多分辨率适配UI
    - 歌词滚动与高亮
    - 播放列表，包括调整歌曲顺序及删除歌曲
- Changed
    - 使用[Zenject](https://github.com/modesttree/Zenject)框架搭建项目
    - 更换了Canvas Scaler的引用分辨率为720x1280，重新调整了UI布局
    - 加入了对于会员及无版权歌曲的播放判断
    - 添加了歌词显示、歌词高亮及歌词滚动的效果，根据专辑封面颜色平均值更换歌词背景颜色
    - 增加了播放队列的功能，可以根据UI调整列表的顺序及删减元素
    - 增加了上一曲、下一曲、播放和暂停的功能
    - 对歌曲返回结果进行相关度排序
    - 对登录状态进行续存（可能是，实际需登录次数减少，但未统计登录间隔时间）
    - 分离代码模块的功能到多个脚本中
    - 修复之前版本的Bug
- Bugs
    - 对于某些近黑色或近白色专辑封面更换歌词背景后会看不清歌词
    - 在低宽度分辨率下，长歌词会因UI高度不够而无法完全显示（需要在解析歌词的时候使用PreferredHeight属性得到UI高度，但是需要等待一帧，由于使用Async-Await无法等待，需替换为协程）
    - 当快速添加多个曲目时，先加载完毕的先排队进入列表，会导致列表顺序不会与添加顺序一致
- Dependence
    - [LitJson](https://github.com/LitJSON/litjson)
    - [QRCode.Unity](https://github.com/codebude/QRCoder.Unity)
    - [UniRx](https://github.com/neuecc/UniRx)
    - [AsyncAwaitUtil](https://github.com/modesttree/Unity3dAsyncAwaitUtil)
    - [Zenject](https://github.com/modesttree/Zenject)

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
