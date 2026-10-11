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

        string lowPath = Path.Combine(
            project, "GameUserSettings.low.ini");

        string highPath = Path.Combine(
            project, "GameUserSettings.high.ini");

        if (!File.Exists(lowPath) || !File.Exists(highPath))
        {
            Console.WriteLine("Р¤Р°Р№Р»С‹ РЅР°СЃС‚СЂРѕРµРє РЅРµ РЅР°Р№РґРµРЅС‹.");
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

        report.AppendLine("# Black Myth: Wukong Benchmark вЂ” СЂРµР·СѓР»СЊС‚Р°С‚С‹");
        report.AppendLine();
        report.AppendLine("## РўРµСЃС‚РѕРІС‹Р№ РєРѕРјРїСЊСЋС‚РµСЂ");
        report.AppendLine();
        report.AppendLine("- РћРЎ: Windows 10 (64-bit)");
        report.AppendLine("- РџСЂРѕС†РµСЃСЃРѕСЂ: Intel Core i5-12500H");
        report.AppendLine("- Р’РёРґРµРѕРєР°СЂС‚Р°: NVIDIA GeForce RTX 3060 Laptop GPU");
        report.AppendLine("- Р’РёРґРµРѕРїР°РјСЏС‚СЊ: 6 GB");
        report.AppendLine("- РћРїРµСЂР°С‚РёРІРЅР°СЏ РїР°РјСЏС‚СЊ: 16 GB");
        report.AppendLine("- Р Р°Р·СЂРµС€РµРЅРёРµ: 1920 Г— 1080");
        report.AppendLine();
        report.AppendLine("## Р РµР·СѓР»СЊС‚Р°С‚С‹");
        report.AppendLine();
        report.AppendLine("| РњРµС‚СЂРёРєР° | LOW | HIGH |");
        report.AppendLine("|---|---:|---:|");
        report.AppendLine("| РЎСЂРµРґРЅРёР№ FPS | 107 | 53 |");
        report.AppendLine("| РњР°РєСЃРёРјР°Р»СЊРЅС‹Р№ FPS | 163 | 74 |");
        report.AppendLine("| РњРёРЅРёРјР°Р»СЊРЅС‹Р№ FPS | 65 | 39 |");
        report.AppendLine("| 5-Р№ РїРµСЂС†РµРЅС‚РёР»СЊ FPS | 91 | 44 |");
        report.AppendLine();
        report.AppendLine("## РќР°СЃС‚СЂРѕР№РєРё РіСЂР°С„РёРєРё");
        report.AppendLine();
        report.AppendLine("| РџР°СЂР°РјРµС‚СЂ | LOW | HIGH |");
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
        report.AppendLine("## РЎРєСЂРёРЅС€РѕС‚С‹");
        report.AppendLine();
        report.AppendLine("### LOW");
        report.AppendLine("![LOW](cpu-low.png)");
        report.AppendLine();
        report.AppendLine("### HIGH");
        report.AppendLine("![HIGH](gpu-high.png)");
        report.AppendLine();
        report.AppendLine(
            "РџСЂРёРјРµС‡Р°РЅРёРµ: LOW Рё HIGH вЂ” СЃС†РµРЅР°СЂРёРё СЃ РЅРёР·РєРёРјРё Рё " +
            "РІС‹СЃРѕРєРёРјРё РЅР°СЃС‚СЂРѕР№РєР°РјРё РіСЂР°С„РёРєРё СЃРѕРѕС‚РІРµС‚СЃС‚РІРµРЅРЅРѕ.");

        string reportPath = Path.Combine(results, "report.md");

        File.WriteAllText(
            reportPath,
            report.ToString(),
            new UTF8Encoding(false)
        );

        Console.WriteLine("РћС‚С‡С‘С‚ СЃРѕР·РґР°РЅ: " + reportPath);
    }
}
