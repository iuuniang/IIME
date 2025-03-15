# IIME

KeePass Password Safe 2 的中文输入法切换插件

[说明](#说明) | [更新日志](#更新日志) | [原理](#原理) | [操作步骤](#操作步骤) 

## 说明

使用 KeePass 多年，在自动输入账号密码时，经常被中文状态的输入法所困扰，经常需要手动切换为英文输入法，十分麻烦。为了解决这个问题，开发了此插件，部分代码参考了 [aardio 输入法与键盘状态检测](https://www.aardio.com/zh-cn/doc/library-guide/std/key/imeState.html) 的实现，在此特别感谢其作者 [Jacen He](https://github.com/aardio) 的开源。

## 更新日志
### [1.1.0] - 2025-3-16
- 懒惰了挺长一段时间，终于修复了热心朋友反馈的[触发和切换的问题](https://github.com/iuuniang/IIME/issues/4)。
- 升级了项目目标框架，从 .NET Framework 3.5 到 .NET Framework 4.7.2。
- 提供PLGX插件。
- 调整占位符号为 `{IME:CN}` `{IME:EN}`。
- 优化了输入法切换的逻辑，兼容性待测试。

### [1.0.0] - 2022-7-30
- 首次发布。
- 采用较为简单的切换方法，存在部分问题。
- 占位符号为 `{IME:ON}` `{IME:OFF}`


## 原理
其实就是检测输入法状态，然后后台发送 `SHIFT键` 进行中英文切换。
AutoType.FilterCompilePre 下返回 `{VKEY 16}` 用来代替 `SHIFT键` 在编译击键序列的时候就会按下该按键。

考虑到也许有的朋友会使用其他输入法，切换中英文状态并不是 `SHIFT键`，因此在占位符号中添加了 **{IME:CN@X} {IME:CN@X}** 的扩展用法，**X** 的取值请参考 [Microsoft文档-Virtual-Key Codes](https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes)，将 **Value** 转为 **十进制** 替换 **X**。暂时仅支持单键。

默认状态使用 **{IME:EN} {IME:CN}** 即可，其VKEY取值为16。

## 操作步骤
- 安装插件。将下载的 `IIME.PLGX` 文件放入 **KeePass** 插件目录下。
- 重新启动 **KeePass**程序。
- *编辑顶级群组—自动输入—替代默认序列为：`{DELAY 100}{CLEARFIELD}{IME:EN}{USERNAME}{TAB}{PASSWORD}{DELAY 100}{ENTER}{IME:CN}`—确定* 即可，其余的子群组都可以继承这个默认序列。

## 备注
编译PLGX的方法：```.\KeePass.exe --plgx-create D:\IIME --plugx-prereq-os:Windows```
