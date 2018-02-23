@echo off
cd cache
cd br_encryptor
echo 正在复制新文件...
taskkill /f /im br_encryptor.exe
taskkill /f /im br_updater.exe
xcopy /Y br_encryptor.exe ..\..\
xcopy /Y br_updater.exe ..\..\
xcopy /i /Y extractor ..\..\extractor
echo 正在删除一些不必要的旧文件...
del ..\..\Mahapps.Metro.dll
del ..\..\Mahapps.Metro.xml
del ..\..\System.IO.Compression.dll
del ..\..\System.IO.Compression.FileSystem.dll
del ..\..\System.Shim.dll
del ..\..\System.Windows.Interactivity.dll
echo 正在清理垃圾...
del br_encryptor.exe
del br_updater.exe
rd /s /q extractor
start "" "..\..\br_encryptor.exe"