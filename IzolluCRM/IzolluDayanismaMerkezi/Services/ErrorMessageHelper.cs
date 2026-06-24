namespace IzolluVakfi.Services;

public static class ErrorMessageHelper
{
    public static Exception GetInnermost(Exception ex)
    {
        var current = ex;
        while (current.InnerException != null)
            current = current.InnerException;
        return current;
    }

    public static string GetFriendlyMessage(Exception ex)
    {
        var raw = GetInnermost(ex).Message;

        if (raw.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase))
        {
            if (raw.Contains("SicilNumarasi", StringComparison.OrdinalIgnoreCase))
                return "Bu sicil numarası zaten kayıtlı.";
            if (raw.Contains("StudentMeetingAttendances", StringComparison.OrdinalIgnoreCase))
                return "Bu öğrenci için aynı toplantıda katılım kaydı zaten mevcut.";
            return "Bu kayıt zaten mevcut (benzersiz alan çakışması).";
        }

        if (raw.Contains("FOREIGN KEY constraint failed", StringComparison.OrdinalIgnoreCase)
            || raw.Contains("relationship has been severed", StringComparison.OrdinalIgnoreCase)
            || raw.Contains("has been severed", StringComparison.OrdinalIgnoreCase))
        {
            return "Bu kayıt başka kayıtlarla (örn. burs ödemesi, transkript, toplantı katılımı) ilişkili olduğu için silinemiyor.";
        }

        if (raw.Contains("NOT NULL constraint failed", StringComparison.OrdinalIgnoreCase))
        {
            return "Zorunlu bir alan boş bırakıldığı için kayıt yapılamadı.";
        }

        return raw;
    }

    public static string GetTechnicalDetail(Exception ex)
    {
        var parts = new List<string>();
        var current = ex;
        while (current != null)
        {
            parts.Add($"{current.GetType().Name}: {current.Message}");
            current = current.InnerException;
        }
        parts.Add(ex.StackTrace ?? string.Empty);
        return string.Join(" -> ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }
}
