# IIME

KeePass Password Safe 2 的中文输入法切换插件

[说明](#说明) | [更新日志](#更新日志) | [原理](#原理) | [操作步骤](#操作步骤) 

## 说明

在使用 KeePass 自动输入账号密码时，若系统处于中文输入状态，容易导致输入内容出错，此时需要手动切换为英文输入法。

只需将 IIME 放入 Plugins 文件夹并重启程序，插件即会自动加载，无需额外配置。该插件能够在输入账号密码前自动切换至英文输入法，输入完成后自动切回中文，整个过程无需人工干预，有效避免因输入法状态导致的手动切换麻烦。

部分代码参考了 [aardio 输入法与键盘状态检测](https://www.aardio.com/zh-cn/doc/library-guide/std/key/imeState.html) 的实现，在此特别感谢其作者 [Jacen He](https://github.com/aardio) 的开源。

## 更新日志
### [1.3.0] - 2025-10-30
- 重构输入法中英文状态切换逻辑。
- 移除 `{IME:CN}` `{IME:EN}` 占位符，现在程序会自动切换了。
- 修复了微软拼音输入法无法切回中文状态的问题。
- 测试了小鹤音形输入法、冰凌输入法等。

### [1.2.0] - 2025-6-11
- 扩展了占位符号，现在支持组合按键（最多3个功能键+1个主键，用空格进行区分）
- 例如{IME:EN@91 32}表示 Win+空格
- {IME:EN@91 32}{DELAY 100}{IME:EN}，理论上表示切换输入法，然后按下Shift键

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
阅读了一下 KeePass.Util.AutoType 的方法，发现有AutoType.FilterSendPre、AutoType.SendPost，对应的就是发送前和发送后。
在发送前关闭中文状态、发送后打开中文就可以了。


## 操作步骤
- 安装插件。将下载的 `IIME.PLGX` 文件放入 **KeePass** 插件目录下。
- 重新启动 **KeePass**程序，会后台自动处理输入法状态，无需任何额外操作。
- 使用前一版本的需要移除旧的 `{IME:CN}` `{IME:EN}`
- *编辑顶级群组—自动输入—替代默认序列为：`{DELAY 100}{CLEARFIELD}{USERNAME}{TAB}{PASSWORD}{DELAY 100}{ENTER}`—确定* 即可，其余的子群组都可以继承这个默认序列。

## 备注
编译PLGX的方法：```.\KeePass.exe --plgx-create D:\IIME --plugx-prereq-os:Windows```
