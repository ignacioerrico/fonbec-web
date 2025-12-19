using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fonbec.Web.DataAccess.Entities.Abstract;

namespace Fonbec.Web.DataAccess.Entities;
public class Sponsor : UserWithoutAccount
{
    public string Email { get; set; } = string.Empty;
    public string? SendAlsoTo { get; set; }
    public string BranchOffice { get; set; } = string.Empty;
}
