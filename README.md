# InnerMediaPlayer

- [概述](#概述)
- [截图](#截图)
- [功能](#功能)

## 概述

**拒绝商业用途**<br>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;基于Unity与网易云WebAPI的播放器，素材来自互联网，可以内嵌入其他Unity项目中，但请一定不要商用！<br>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;目前只支持使用网易云App扫码登录，登录产生的Cookie会存到persistentPath的Cookie.json中，联网动作只会产生于登录、搜索、播放的操作，账号信息不会泄露。<br>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;注意：<del>不登录只能返回20个搜索结果</del><font color = "gray" size = "2">（网易云你真该死啊，现在不登陆一个结果都不给返回）</font>，非黑胶会员仍然不能播放会员歌曲。以下为软件内部截图。<br>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<del>有时候修改完一些小问题以合懒得发布Release版本，攒的小更新多了之后一并发布新版本，有迫切需求的请自己build。</del>

## 截图

![img1](Documentation/Image/img1.png)
![img2](Documentation/Image/img2.png)
![guide1](Documentation/Image/guide1.png)
![guide2](Documentation/Image/guide2.png)

## 功能

- 现有功能
    - 扫码登录账号
    - 歌词显示及滚动
    - 搜索歌曲、艺人和专辑栏
    - 播放列表
    - 播放/暂停/上一曲/下一曲
    - 歌曲播放进度条
    - JSON批量导入歌曲
- 未来将会添加
    - 重新整理UI布局
    - 发布个人歌单
- 明确不会添加的功能
    - 查看或发表评论
    - 播放MV等视频

---

<div align="center">
    <img src="Documentation/Image/icon.png" title="为什么图标是这个，因为找不到喜欢的歌哭死在里面" height=100 width=100><br>
    <body>↑软件图标↑</body>
</div>