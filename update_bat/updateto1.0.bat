@echo off
cd cache
cd br_encryptor
echo ���ڸ������ļ�...
taskkill /f /im br_encryptor.exe
taskkill /f /im br_updater.exe
xcopy /Y br_encryptor.exe ..\..\
xcopy /Y br_updater.exe ..\..\
xcopy /i /Y extractor ..\..\extractor
echo ����ɾ��һЩ����Ҫ�ľ��ļ�...
del ..\..\Mahapps.Metro.dll
del ..\..\Mahapps.Metro.xml
del ..\..\System.IO.Compression.dll
del ..\..\System.IO.Compression.FileSystem.dll
del ..\..\System.Shim.dll
del ..\..\System.Windows.Interactivity.dll
echo ������������...
del br_encryptor.exe
del br_updater.exe
rd /s /q extractor
start "" "..\..\br_encryptor.exe"