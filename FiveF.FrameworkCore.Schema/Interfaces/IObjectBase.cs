// TCDev 2022/03/16
// Apache 2.0 License
// https://www.github.com/deejaytc/dotnet-utils

using System.ComponentModel.DataAnnotations;

namespace TCDev.ApiGenerator.Interfaces
{
    public interface IObjectBase<TId>
   {
      [Key]
      TId Id { get; set; }
   }
}