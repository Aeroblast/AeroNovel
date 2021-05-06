dotnet publish -p:Configuration=Release -r win10-x64
rd /Q /S app
md app
move bin\release\net5\win10-x64\publish\AeroNovelTool.exe app\AeroNovelTool.exe
del template.zip
"C:\Program Files\7-Zip\7z.exe" a template.zip ".\template\*"
copy template.zip app\template.zip
copy template_bat\drop_atxt_atxt2bbcode.txt app\drop_atxt_atxt2bbcode.bat
copy template_bat\drop_epub_epub2comment.txt app\drop_epub_epub2comment.bat
copy template_bat\drop_folder_bbcode.txt app\drop_folder_bbcode.bat
copy template_bat\drop_folder_epub.txt app\drop_folder_epub.bat
copy template_bat\drop_epub_epub2comment_with_magic.txt app\drop_epub_epub2comment_with_magic.bat
copy template_bat\drop_atxt-or-folder_atxt2inlinehtml.txt app\drop_atxt-or-folder_atxt2inlinehtml.bat
pause