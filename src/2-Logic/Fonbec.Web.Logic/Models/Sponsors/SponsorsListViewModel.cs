using Fonbec.Web.DataAccess.DataModels.Sponsors;
using Fonbec.Web.DataAccess.DataModels.Students;
using Fonbec.Web.Logic.Models.Students;
using Mapster;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fonbec.Web.Logic.Models.Sponsors;
    public class SponsorsListViewModel : AuditableViewModel
    {
        public int SponsorId { get; set; } 

        public string SponsorFirstName { get; set; } = string.Empty;

        public string SponsorLastName { get; set; } = string.Empty;

        public string SponsorNickName { get; set; } = string.Empty;

        public string SponsorEmail { get; set; } = string.Empty;

        // added
        public int SponsorChapterId { get; set; }

        public string SponsorPhoneNumber { get; set; } = string.Empty;
 
    }

    public class SponsorsListViewModelMappingDefinitions : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
        config.NewConfig<AllSponsorsDataModel, SponsorsListViewModel>()
            .Map(dest => dest.SponsorId, src => src.SponsorId)
            .Map(dest => dest.SponsorFirstName, src => src.SponsorFirstName)
            .Map(dest => dest.SponsorLastName, src => src.SponsorLastName)
            .Map(dest => dest.SponsorNickName, src => src.SponsorNickName ?? string.Empty)
            .Map(dest => dest.SponsorEmail, src => src.SponsorEmail ?? string.Empty)
            // added
            .Map(dest => dest.SponsorChapterId, src => src.SponsorChapterId)
            .Map(dest => dest.SponsorPhoneNumber, src => src.SponsorPhoneNumber);
        }
    }

