using System.CommandLine;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using MiliastraUtility.Core;

namespace MiliastraUtility.CLI.Commands;

public class ConvertCommand
{
    static readonly JsonSerializerOptions options = new()
    {
        // 美化输出
        WriteIndented = true,
        // 将属性名转为小驼峰
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        // 防止 Unicode 字符被转为 `\uXXXX`
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        Converters =
        {
            new JsonStringEnumConverter<GiFileType>(JsonNamingPolicy.CamelCase),
            new JsonStringEnumConverter()
        },
    };

    public static Command Create()
    {
        var command = new Command("convert", "转换文件的类型");
        command.Aliases.Add("conv");
        command.SetAction(Action);

        var inputOption = new Option<FileInfo>("--input", "-i")
        {
            Description = "待转换的文件",
            HelpName = "file",
            Required = true,
        };
        inputOption.Validators.Add(result =>
        {
            var file = result.GetValue(inputOption);
            if (file is null) { result.AddError("未指定必要的参数"); return; }
            if (!file.Exists) { result.AddError("指定的文件不存在"); return; }
            string ext = file.Extension.ToLower();
            if (!IsSupportedFileFormat(ext))
            {
                result.AddError($"文件类型不受支持，必须为 .json 或 .gia，但得到了：{ext}");
            }
        });
        command.Options.Add(inputOption);

        var outputOption = new Option<DirectoryInfo>("--output", "-o")
        {
            DefaultValueFactory = _ => new (Directory.GetCurrentDirectory()),
            Description = "导出目录，未指定时为当前目录",
            HelpName = "dir",
        };
        outputOption.Validators.Add(result =>
        {
            var dir = result.GetValue(outputOption)!;
            if (!dir.Exists) // 确保后续导出文件时的路径一定合法
            {
                try
                { Directory.CreateDirectory(dir.FullName); }
                catch
                { result.AddError("指定的目录不存在，且在尝试创建该目录时发生了错误"); }
            }
        });
        command.Options.Add(outputOption);

        return command;
    }

    static bool IsSupportedFileFormat(string ext)
    {
        return ext switch
        {
            ".json" => true,
            ".gia" => true,
            _ => false,
        };
    }

    static async Task Action(ParseResult result, CancellationToken token)
    {
        var file = result.GetValue<FileInfo>("--input")!;
        var dir = result.GetValue<DirectoryInfo>("--output")!;
        string ext = file.Extension.ToLower();
        var func = ext switch
        {
            ".gia" => ConvertGiaToJson,
            ".json" => ConvertJsonToGiFile,
            // 这行只是用来帮 switch 推断类型用的，永远不会被执行到
            _ => (Func<FileInfo, DirectoryInfo, Task>)NoEffect
        };
        await func(file, dir);
    }

    static async Task ConvertGiaToJson(FileInfo file, DirectoryInfo dir)
    {
        var gia = GiaFile.ReadFromFile(file.FullName);
        string json = JsonSerializer.Serialize(gia, options);
        string path = string.Concat(dir.FullName, Path.GetFileNameWithoutExtension(file.Name), ".json");
        await File.WriteAllTextAsync(path, json);
    }

    static async Task ConvertJsonToGiFile(FileInfo file, DirectoryInfo dir)
    {
        ReadOnlySpan<byte> data = await File.ReadAllBytesAsync(file.FullName);
        var reader = new Utf8JsonReader(data);
        var type = GiFileType.Unknown;
        while (reader.Read())
        {
            // 只关心根属性的值
            if (reader.CurrentDepth != 1) continue;
            // 先获取文件类型，再决定解析分支
            if (reader.TokenType == JsonTokenType.PropertyName &&
                reader.GetString() == "type")
            {
                reader.Read();
                type = reader.GetString() switch
                {
                    "gip" => GiFileType.Gip,
                    "gil" => GiFileType.Gil,
                    "gia" => GiFileType.Gia,
                    "gir" => GiFileType.Gir,
                    _ => GiFileType.Unknown
                };
                break;
            }
        }

        switch (type)
        {
            case GiFileType.Gip: throw new NotSupportedException();
            case GiFileType.Gil: throw new NotSupportedException();
            case GiFileType.Gia:{
                string path = string.Concat(dir.FullName, "\\", Path.GetFileNameWithoutExtension(file.Name), ".gia");
                var gia = JsonSerializer.Deserialize<GiaFile>(data, options)!;
                gia.WriteToFile(path);
                break;
            }
            case GiFileType.Gir: throw new NotSupportedException();
            default: throw new NotSupportedException();
        }
    }

    static async Task NoEffect(FileInfo file, DirectoryInfo dir) { }
}
