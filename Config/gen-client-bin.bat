dotnet.exe ./Tools/Luban.dll --target client --dataTarget bin --codeTarget cs-bin --xargs outputDataDir=../Client/Assets//Bundles/Config  --xargs outputCodeDir=../Client/Assets//Hotfix/Config/Generate --xargs tableImporter.name=gameframex -x l10n.provider=gameframex -x l10n.textFile.keyFieldName=key  -x l10n.textFile.path=./Excels/Local/ --conf ./Luban.conf

pause