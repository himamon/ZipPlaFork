with (WScript)
with (new ActiveXObject("Scripting.FileSystemObject"))
{
    var projectDir = Arguments(0);
    for (var files = new Enumerator(GetFolder(projectDir).Files) ; !files.atEnd() ; files.moveNext()) {
        var file = files.item() + "";
        if (/\.Designer\.cs$/i.test(file))
        {
            var bakFile = file + ".bak";
            if (FileExists(bakFile))
            {
                DeleteFile(file, true);
                MoveFile(bakFile, file);
            }
        }
    }

    Echo(ScriptName + " finished!");
}
