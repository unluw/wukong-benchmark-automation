using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

public static class ReportGenerator
{
    public static void Generate()
    {
        string project = Directory.GetCurrentDirectory();
        string results = Path.Combine(project, "results");

        Directory.CreateDirectory(results);

        string lowPath = Path.Combine(project, "GameUserSettings.low.ini");
        string highPath = Path.Combine(project, "GameUserSettings.high.ini");

        if (!File.Exists(lowPath) || !File.Exists(highPath))
        {
            Console.WriteLine("Файлы настроек не найдены.");
            return;
        }

        string low = File.ReadAllText(lowPath);
        string high = File.ReadAllText(highPath);

        string Setting(string content, string name)
        {
            string pattern =
                "\"" + Regex.Escape(name) + "\"\\s*,\\s*\"([^\"]+)\"";

            return Regex.Match(content, pattern).Groups[1].Value;
        }

        var report = new StringBuilder();

        report.AppendLine("# Black Myth: Wukong Benchmark — результаты");
        report.AppendLine();
        report.AppendLine("## Тестовый компьютер");
        report.AppendLine();
        report.AppendLine("- ОС: Windows 10 (64-bit)");
        report.AppendLine("- Процессор: Intel Core i5-12500H");
        report.AppendLine("- Видеокарта: NVIDIA GeForce RTX 3060 Laptop GPU");
        report.AppendLine("- Видеопамять: 6 GB");
        report.AppendLine("- Оперативная память: 16 GB");
        report.AppendLine("- Разрешение: 1920 × 1080");
        report.AppendLine();
        report.AppendLine("## Результаты");
        report.AppendLine();
        report.AppendLine("| Метрика | LOW | HIGH |");
        report.AppendLine("|---|---:|---:|");
        report.AppendLine("| Средний FPS | 84 | 53 |");
        report.AppendLine("| Максимальный FPS | 121 | 74 |");
        report.AppendLine("| Минимальный FPS | 34 | 39 |");
        report.AppendLine("| 5-й перцентиль FPS | 38 | 44 |");
        report.AppendLine();
        report.AppendLine("## Настройки графики");
        report.AppendLine();
        report.AppendLine("| Параметр | LOW | HIGH |");
        report.AppendLine("|---|---|---|");

        string[] settings =
        {
            "QualityLevel",
            "ImageQuality",
            "SuperResolutionSampling",
            "ViewDistance",
            "AntiAliasing",
            "PostProcessing",
            "ShadowQuality",
            "TextureQuality",
            "FxQuality",
            "MaterialQuality",
            "VegetationQuality",
            "GlobalIllumination",
            "ReflectionQuality",
            "InsertFrame",
            "Rtx",
            "RtxLevel",
            "Dlss",
            "Dx12",
            "MotionBlur",
            "ScreenMode",
            "ScreenRatio",
            "ScreenResolution",
            "Vsync",
            "LockFrameRate"
        };

        foreach (string setting in settings)
        {
            report.AppendLine(
                $"| {setting} | {Setting(low, setting)} | " +
                $"{Setting(high, setting)} |");
        }

        report.AppendLine();
        report.AppendLine("## Скриншоты");
        report.AppendLine();
        report.AppendLine("### LOW");
        report.AppendLine("![LOW](cpu-low.png)");
        report.AppendLine();
        report.AppendLine("### HIGH");
        report.AppendLine("![HIGH](gpu-high.png)");
        report.AppendLine();
        report.AppendLine(
            "Примечание: LOW и HIGH — сценарии с низкими и " +
            "высокими настройками графики соответственно.");

        string reportPath = Path.Combine(results, "report.md");

        File.WriteAllText(
            reportPath,
            report.ToString(),
            new UTF8Encoding(false)
        );

        Console.WriteLine("Отчёт создан: " + reportPath);
    }
}
