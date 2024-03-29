with (WScript)
with (new ActiveXObject("Scripting.FileSystemObject"))
{
    var projectDir = Arguments(0);
    var first = true;
    for (var files = new Enumerator(GetFolder(projectDir).Files) ; !files.atEnd() ; files.moveNext())
    {
        var file = files.item() + ""; // 文字列型へ明示的にキャスト
        if (/\.Designer\.cs$/i.test(file))
        {
            var stream = WScript.CreateObject("ADODB.Stream");
            stream.Type = 2; // Text
            stream.Charset = "UTF-8";

            stream.Open();
            stream.LoadFromFile(file);
            var input = stream.ReadText();
            stream.Close();

            var exceptions = /^(?:[^a-zA-Z]+|.*%[A-Z]+%.*|&About|\w+\.(?:exe|EXE)|Lanczos\d|Spline\d+)$/;
            var output = input.replace(/^(\s*)((?:\w+\.)+Text = "(.*)";)$/gm,
                function($0, $1, $2, $3)
                {
                    return exceptions.test($3) ? $0 : $1 + "// " + $2;
                }
            );

            var fileName = GetFileName(file);
            var layouts =
                fileName == "CatalogForm.Designer.cs" ?
                    ["menuStrip", "menuStripLeft"] :
                fileName == "ViewerForm.Designer.cs" ?
                    ["menuStrip"] :
                    null;
            if (layouts != null)
            {
                output = output.replace(new RegExp("^(\\s+)(this\\.(?:" + layouts.join('|') + ")\\.PerformLayout\\(\\);)$", "gm"), "$1// $2");
            }

            var bakFile = file + ".bak";
            MoveFile(file, bakFile);

            stream.Open();
            stream.WriteText(output);
            stream.SaveToFile(file);
            stream.Close();
        }
    }

    Echo(ScriptName + " finished!");
}
