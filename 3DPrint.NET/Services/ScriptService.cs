using Microsoft.AspNetCore.Components;

namespace _3DPrint.NET.Services;
public class ScriptService {
    private List<string> _scripts = new List<string>();
    public IEnumerable<string> GetUrls() {
        return _scripts;
    }

    public void AddUrl(string url) {
        _scripts.Add(url);
    }

    public void AddUrls(string[] urls) {
        _scripts.AddRange(urls);
    }
}
