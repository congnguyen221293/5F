// TCDev 2022/03/16
// Apache 2.0 License
// https://www.github.com/deejaytc/dotnet-utils

namespace TCDev.ApiGenerator.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Enum)]
   public class SwaggerIgnoreAttribute : Attribute
   {
      public SwaggerIgnoreAttribute()
      {
      }
   }
}