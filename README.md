# UnityZip
使用SharpZipLib实现Unity压缩/解压缩

Unity版本556, SharpZipLib 0.8
如果使用Unity2018.3以后的版本 可以自己把SharpZipLib升级到最新, 最新版需要C#7.0的语法支持

解压时安卓上会分配大量的mono堆内存.

大概每10mb 需要最多 1s 的解压时间.

只测试了zip格式.
