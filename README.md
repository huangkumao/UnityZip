# UnityZip
使用SharpZipLib实现Unity压缩/解压缩

Unity版本556, SharpZipLib 0.8
如果使用Unity2018.3以后的版本 可以自己把SharpZipLib升级到最新, 最新版需要C#7.0的语法支持

解压时安卓上会分配大量的mono堆内存.

测试压缩包200mb, 包内全是几十k的png图片.
大概每10mb 需要最多 1s 的解压时间. 也就是 200mb的zip文件 解压需要18-21左右.

正常项目中使用 需要放到多线程中进行解压缩操作.

Unity-Logs-Viewer方便查看日志和内存占用.

只测试了zip格式.



**New 升级到了2018.3 SharpZipLib 也升级到了最新版 1.10**

![](https://github.com/huangkumao/GitProjectImgs/blob/master/UnityZip/1.png) 
![](https://github.com/huangkumao/GitProjectImgs/blob/master/UnityZip/2.png) 

