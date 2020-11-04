@ECHO off
dotnet publish -c Release -r win10-x64 -o bin\Publish\win10-x64\ -p:SelfContained=true -p:PublishSingleFile=true -p:PublishReadyToRun=true -p:PublishTrimmed=true