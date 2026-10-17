using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
namespace MacroExecute
{
    // Groups OR together; clauses AND together; terms within a clause OR together.
    public static class ClipboardRules
    {
        public static string Quote(string term){return "\""+term.Replace("\\","\\\\").Replace("\"","\\\"")+"\"";}
        public static string[] Terms(string row)
        {
            var terms=new List<string>();int i=0;
            while(i<row.Length){while(i<row.Length&&char.IsWhiteSpace(row[i]))i++;if(i==row.Length)break;var value=new StringBuilder();
                if(row[i]=='"'){i++;bool closed=false;while(i<row.Length){char c=row[i++];if(c=='"'){closed=true;break;}if(c=='\\'&&i<row.Length&&(row[i]=='"'||row[i]=='\\'))c=row[i++];value.Append(c);}if(!closed)throw new InvalidDataException("Close the quote around the phrase in this row.");if(i<row.Length&&!char.IsWhiteSpace(row[i]))throw new InvalidDataException("Separate alternatives with a space after a quoted phrase.");}
                else{while(i<row.Length&&!char.IsWhiteSpace(row[i])){if(row[i]=='"')throw new InvalidDataException("Start a quoted phrase after a space, or quote the whole string.");value.Append(row[i++]);}}
                if(value.Length==0)throw new InvalidDataException("Empty quotes cannot be a clipboard string. Enter text between the quotes.");terms.Add(value.ToString());
            }
            if(terms.Count==0)throw new InvalidDataException("Fill in every string row, or remove unused rows.");return terms.ToArray();
        }
        public static string[][][] Parse(string text)
        {
            var groups=new List<string[][]>();var clauses=new List<string[]>();var terms=new List<string>();string connector=null;int total=0,line=0;
            foreach(string raw in (text??"").Replace("\r\n","\n").Replace('\r','\n').Split('\n')){
                line++;string row=raw.Trim();if(row.Length==0)continue;
                if(row=="AND"||row=="OR"||row=="GROUP OR"){if(terms.Count==0||connector!=null)throw new InvalidDataException("A connector needs a filled string row on both sides (line "+line+").");connector=row;continue;}
                string[] parsed;try{parsed=Terms(row);}catch(InvalidDataException error){throw new InvalidDataException("String row at line "+line+": "+error.Message);}
                if(terms.Count>0&&connector==null)throw new InvalidDataException("Choose OR, AND or GROUP OR between string rows (line "+line+").");
                if(connector=="AND"||connector=="GROUP OR"){clauses.Add(terms.ToArray());terms.Clear();}
                if(connector=="GROUP OR"){groups.Add(clauses.ToArray());clauses.Clear();}
                terms.AddRange(parsed);total+=parsed.Length;if(total>128)throw new InvalidDataException("Use at most 128 clipboard strings per condition.");connector=null;
            }
            if(connector!=null)throw new InvalidDataException("Fill the string row after "+connector+".");if(terms.Count==0)throw new InvalidDataException("Enter at least one string to check.");clauses.Add(terms.ToArray());groups.Add(clauses.ToArray());return groups.ToArray();
        }
        public static string FromLegacy(Step step)
        {
            if(step.ClipboardMatch==ClipboardMatchMode.Rules)return step.Text;
            var groups=ClipboardCondition.Groups(step);if(groups.Length==0)return "";
            if(step.ClipboardMatch==ClipboardMatchMode.Any)return string.Join(" ",groups.SelectMany(g=>g).Select(Quote));
            return string.Join("\nGROUP OR\n",groups.Select(g=>string.Join("\nAND\n",g.Select(Quote))));
        }
        private static bool Assign(int row,string[][] candidates,Dictionary<string,int> owner,HashSet<string> visited)
        {
            foreach(string term in candidates[row]){if(!visited.Add(term))continue;int other;if(!owner.TryGetValue(term,out other)||Assign(other,candidates,owner,visited)){owner[term]=row;return true;}}return false;
        }
        public static string[] SharedTerms(string[][] clauses,bool ignoreCase)
        {var comparer=ignoreCase?StringComparer.OrdinalIgnoreCase:StringComparer.Ordinal;var seen=new HashSet<string>(comparer);var shared=new HashSet<string>(comparer);foreach(var clause in clauses)foreach(string term in clause.Distinct(comparer))if(!seen.Add(term))shared.Add(term);return shared.ToArray();}
        public static bool Matches(string text,string[][][] groups,StringComparison comparison,int[] reuseGroups=null)
        {
            var comparer=comparison==StringComparison.OrdinalIgnoreCase?StringComparer.OrdinalIgnoreCase:StringComparer.Ordinal;
            for(int groupIndex=0;groupIndex<groups.Length;groupIndex++){var group=groups[groupIndex];var candidates=group.Select(row=>row.Where(term=>text.IndexOf(term,comparison)>=0).Distinct(comparer).ToArray()).ToArray();if(candidates.Any(row=>row.Length==0))continue;if(reuseGroups!=null&&reuseGroups.Contains(groupIndex))return true;var owner=new Dictionary<string,int>(comparer);bool complete=true;for(int row=0;row<candidates.Length;row++)if(!Assign(row,candidates,owner,new HashSet<string>(comparer))){complete=false;break;}if(complete)return true;}return false;
        }
    }
}
