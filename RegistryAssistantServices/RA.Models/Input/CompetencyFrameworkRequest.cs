﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RA.Models.Input
{
    public class CompetencyFrameworkRequest : BaseRequest
    {
        public CompetencyFrameworkRequest()
        {
            CompetencyFramework = new CompetencyFramework();
        }
        public string CTID { get; set; }
        public CompetencyFramework CompetencyFramework { get; set; }

        //OR,will already be properly formatted, particularly if from CASS

        public CompetencyFrameworksGraph CompetencyFrameworkGraph { get; set; }

    }
    public class CompetencyFramework 
    {
        
        public CompetencyFramework()
        {
        }
        //won't be entered, only one type
        //public string Type { get; set; }

        //required
        public string CtdlId { get; set; }

        //required
        public string Ctid { get; set; }



        public List<string> alignFrom { get; set; } = new List<string>();

        public List<string> alignTo { get; set; } = new List<string>();

        public List<string> author { get; set; } = new List<string>();



        public List<LanguageItem> conceptKeyword { get; set; } = new List<LanguageItem>();

        public List<string> conceptTerm { get; set; } = new List<string>();

        public List<string> creator { get; set; } = new List<string>();

        public string dateCopyrighted { get; set; }

        public string dateCreated { get; set; }


        public string dateValidFrom { get; set; }

        public string dateValidUntil { get; set; }

        public List<string> derivedFrom { get; set; } = new List<string>();

        //???language map??
        public LanguageItem description { get; set; } = new LanguageItem();

        public List<string> educationLevelType { get; set; } = new List<string>();

        public List<string> hasTopChild { get; set; } = new List<string>();

        public List<string> identifier { get; set; } = new List<string>();

        public List<string> inLanguage { get; set; } = new List<string>();

        public string license { get; set; }

        public List<LanguageItem> localSubject { get; set; } = new List<LanguageItem>();


        public LanguageItem name { get; set; } = new LanguageItem();

		public string publicationStatusType { get; set; }

		public List<string> publisher { get; set; } = new List<string>();

        public List<LanguageItem> publisherName { get; set; } = new List<LanguageItem>();
        //

        public string repositoryDate { get; set; }

        public string rights { get; set; }

        public string rightsHolder { get; set; }

        public List<string> source { get; set; } = new List<string>();

        //
        public List<LanguageItem> tableOfContents { get; set; } = new List<LanguageItem>();

        public List<Competency> Competencies { get; set; } = new List<Competency>();
    }

    public class Competency 
    {
        //required": [ "@type", "@id", "ceasn:competencyText", "ceasn:inLanguage", "ceasn:isPartOf", "ceterms:ctid" ]
        
        public Competency()
        {
        }

        public string Type { get; set; }

        public string CtdlId { get; set; }

        public string Ctid { get; set; }

        public List<string> alignFrom { get; set; } = new List<string>();

        public List<string> alignTo { get; set; } = new List<string>();

        public List<string> altCodedNotation { get; set; } = new List<string>();

        public List<string> author { get; set; } = new List<string>();

        public List<string> broadAlignment { get; set; } = new List<string>();

        public string codedNotation { get; set; }


        public LanguageMap comment { get; set; }


        public List<LanguageMap> competencyCategory { get; set; }



        public LanguageMap competencyText { get; set; }

        ///ProficiencyScale??
        public List<string> complexityLevel { get; set; } = new List<string>();

        public List<string> comprisedOf { get; set; } = new List<string>();

        public List<LanguageMap> conceptKeyword { get; set; }

        public List<string> conceptTerm { get; set; } = new List<string>();



        public List<string> creator { get; set; } = new List<string>();

        public List<string> crossSubjectReference { get; set; } = new List<string>();

        public string dateCreated { get; set; }

        public List<string> derivedFrom { get; set; } = new List<string>();

        public List<string> educationLevelType { get; set; } = new List<string>();

        public List<string> exactAlignment { get; set; } = new List<string>();


        public List<Competency> hasChild { get; set; } = new List<Competency>();

        public List<string> identifier { get; set; } = new List<string>();

        public List<string> inLanguage { get; set; } = new List<string>();

        public List<string> isChildOf { get; set; } = new List<string>();

        public List<string> isPartOf { get; set; } = new List<string>();

        public string isVersionOf { get; set; }

        public string listID { get; set; }

        public List<LanguageMap> localSubject { get; set; }

        public List<string> majorAlignment { get; set; } = new List<string>();

        public List<string> minorAlignment { get; set; } = new List<string>();

        public List<string> narrowAlignment { get; set; } = new List<string>();

        public List<string> prerequisiteAlignment { get; set; } = new List<string>();

        public List<string> skillEmbodied { get; set; } = new List<string>();


        public string weight { get; set; }

    }

        
}
