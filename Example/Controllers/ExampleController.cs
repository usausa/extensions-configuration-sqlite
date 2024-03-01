namespace Example.Controllers;

using Extensions.Configuration.Sqlite;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;

[ApiController]
[Route("[controller]/[action]")]
public class ExampleController : ControllerBase
{
    [HttpGet]
    public IActionResult Setting([FromServices] IOptionsSnapshot<DynamicSetting> setting)
    {
        return Ok(setting.Value);
    }

    [HttpPost]
    public async ValueTask<IActionResult> Setting(
        [FromServices] IConfigurationOperator configurationOperator,
        string? value1,
        int? value2)
    {
        await configurationOperator.BulkUpdateAsync(
            new KeyValuePair<string, object?>("Dynamic:Value1", value1),
            new KeyValuePair<string, object?>("Dynamic:Value2", value2)).ConfigureAwait(false);

        return Ok();
    }

    [HttpGet]
    public async ValueTask<IActionResult> Feature([FromServices] IFeatureManager featureManager)
    {
        return Ok(await featureManager.IsEnabledAsync("Custom").ConfigureAwait(false));
    }

    [HttpPost]
    public async ValueTask<IActionResult> Feature(
        [FromServices] IConfigurationOperator configurationOperator,
        bool enable)
    {
        await configurationOperator.UpdateAsync("FeatureManagement:Custom", enable).ConfigureAwait(false);

        return Ok();
    }
}
