using System.Text;
using ForgeAndFade.Api.Models;

namespace ForgeAndFade.Api.Data;

public static class ServiceCatalogue
{
    // Compare case and whitespace consistently without rewriting historical display names.
    public static string NameKey(string name) => string.Join(" ",
        name.Normalize(NormalizationForm.FormKC).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        .ToUpperInvariant();

    public static IEnumerable<Service> ActiveServices(IEnumerable<Service> services) => services
        .Where(service => service.IsActive)
        .OrderBy(service => service.ServiceId)
        .DistinctBy(service => NameKey(service.ServiceName));
}
