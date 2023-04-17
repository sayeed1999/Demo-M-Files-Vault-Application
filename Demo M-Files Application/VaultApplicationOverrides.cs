using MFiles.VAF.Common;
using MFiles.VAF.Configuration.AdminConfigurations;
using MFiles.VAF.Configuration;
using MFilesAPI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo_M_Files_Application
{
    public partial class VaultApplication
    {
        protected override void OnConfigurationUpdated(Configuration oldConfiguration, bool isValid, bool updateExternals)
        {
            base.OnConfigurationUpdated(oldConfiguration, isValid, updateExternals);

            // Build up the string to log.
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Configuration changed:");
            stringBuilder.AppendLine($"Old: {JsonConvert.SerializeObject(oldConfiguration, Formatting.Indented)}");
            stringBuilder.AppendLine($"New: {JsonConvert.SerializeObject(this.Configuration, Formatting.Indented)}");

            // Log the string.
            SysUtils.ReportToEventLog(
                stringBuilder.ToString(),
                EventLogEntryType.Information
            );

        }

        protected override IEnumerable<ValidationFinding> CustomValidation(Vault vault, Configuration config)
        {
            // The base implementation should not return any, but handle it in case that changes in the future.
            var validationFindings =
                new List<ValidationFinding>(base.CustomValidation(vault, config) ?? new List<ValidationFinding>());

            // Sanity.
            if (null == config)
                config = new Configuration();

            // Username must be set.
            if (string.IsNullOrWhiteSpace(config.Username))
                validationFindings.Add(new ValidationFinding(
                    ValidationFindingType.Error,
                    nameof(this.Configuration.Username),
                    "Username cannot be empty"));

            // Password must be set.
            if (string.IsNullOrWhiteSpace(config.Password))
                validationFindings.Add(new ValidationFinding(
                    ValidationFindingType.Error,
                    nameof(this.Configuration.Password),
                    "Password cannot be empty"));

            return validationFindings;
        }

        public override string GetDashboardContent(IConfigurationRequestContext context)
        {
            return $"<h3>This is my dashboard.</h3>";
        }

    }
}
