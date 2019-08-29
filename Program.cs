using System;
using System.Data;
using System.IO;
using System.Text;
using MySql.Data.MySqlClient;

namespace ModelClassBuilder
{
    class Program
    {
        static void Main(string[] args)
        {
            var schema = "UniversalPS";
            //var s = buildClass(table);
            buildClass(schema);
        }

        //public static string buildClass(string table)
        public static void buildClass(string schemaName)
        {
            string path = "classes/";
            string extensionFile = ".cs";
            string connStr = string.Format("Server=localhost; database={0}; UID={1}; password={2}", schemaName, "root", "admin123");
            using (var con = new MySqlConnection(connStr))
            {
                con.Open();
                DataTable allTablesSchemaTable = con.GetSchema("Tables");
                if (allTablesSchemaTable != null)
                {
                    foreach (DataRow row in allTablesSchemaTable.Rows)
                    {
                        DataTable table = row.Table;
                        string tableName = (string)row[2];
                        tableName = tableName.Substring(0, 1).ToUpper() + tableName.Substring(1);

                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine("using System;");
                        sb.AppendLine("");
                        sb.AppendLine("namespace ups_api.Models {");
                        sb.AppendLine("");
                        sb.AppendLine("  public class " + tableName + " {");
                        sb.AppendLine("");

                        string selectCommandText = "select * from " + tableName + " where 1=0"; // No data wanted, only schema  
                        using (MySqlCommand command = new MySqlCommand(selectCommandText, con))
                        {
                            using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                            {
                                DataTable schemaTable = new DataTable("SchemaInformation");
                                adapter.FillSchema(schemaTable, SchemaType.Source);
                                foreach (DataColumn col in schemaTable.Columns)
                                {
                                    string name = col.ColumnName;
                                    string type = col.DataType.Name;
                                    sb.AppendLine("    public " + type + " " + char.ToUpper(name[0]) + name.Substring(1) + " { get; set; }");
                                }
                            }
                        }

                        sb.AppendLine("");
                        sb.AppendLine("  }");
                        sb.AppendLine("");
                        sb.AppendLine("}");

                        FileStream file = new FileStream(path + tableName + extensionFile, FileMode.OpenOrCreate);
                        using (StreamWriter sw = new StreamWriter(file))
                        {
                            sw.Write(sb.ToString());
                        }

                        sb = new StringBuilder();

                        sb.AppendLine("using ups_api.Models;");
                        sb.AppendLine("");
                        sb.AppendLine("namespace ups_api.DAL {");
                        sb.AppendLine("");
                        sb.AppendLine("  public interface I" + tableName + "Service: IGenericService<" + tableName + "> {}");
                        sb.AppendLine("");
                        sb.AppendLine("  public class " + tableName + "Service: GenericService<" + tableName + ">, I" + tableName + "Service {");
                        sb.AppendLine("    public " + tableName + "Service(UPSContext ctx) : base(ctx){}");
                        sb.AppendLine("  }");
                        sb.AppendLine("");
                        sb.AppendLine("}");

                        file = new FileStream(path + tableName + "Service" + extensionFile, FileMode.OpenOrCreate);
                        using (StreamWriter sw = new StreamWriter(file))
                        {
                            sw.Write(sb.ToString());
                        }
                        sb = new StringBuilder();

                        sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
                        sb.AppendLine("using System.Collections.Generic;");
                        sb.AppendLine("using System.Threading.Tasks;");
                        sb.AppendLine("");
                        sb.AppendLine("using ups_api.DAL;");
                        sb.AppendLine("using ups_api.Models;");
                        sb.AppendLine("");
                        sb.AppendLine("namespace ups_api.Controllers {");
                        sb.AppendLine("");
                        sb.AppendLine("  [Route(\"" + tableName.ToLower() + "s\")]");
                        sb.AppendLine("  [ApiController]");
                        sb.AppendLine("  public class " + tableName + "Controller: ControllerBase {");
                        sb.AppendLine("");
                        sb.AppendLine("    public I" + tableName + "Service " + tableName + "Service;");
                        sb.AppendLine("");
                        sb.AppendLine("    public " + tableName + "Controller(I" + tableName + "Service " + tableName.ToLower() + "Service){");
                        sb.AppendLine("      " + tableName + "Service = " + tableName.ToLower() + "Service;");
                        sb.AppendLine("    }");
                        sb.AppendLine("");
                        sb.AppendLine("    [HttpGet]");
                        sb.AppendLine("    public async Task<ActionResult<List<" + tableName + ">>> Get" + tableName + "List(){");
                        sb.AppendLine("");
                        sb.AppendLine("      List<" + tableName + "> result = await " + tableName + "Service.GetList();");
                        sb.AppendLine("      if(result == null || result.Count < 1)");
                        sb.AppendLine("        return NotFound();");
                        sb.AppendLine("      return Ok(result);");
                        sb.AppendLine("");
                        sb.AppendLine("    }");
                        sb.AppendLine("");
                        sb.AppendLine("  }");
                        sb.AppendLine("");
                        sb.AppendLine("}");

                        file = new FileStream(path + tableName + "Controller" + extensionFile, FileMode.OpenOrCreate);
                        using (StreamWriter sw = new StreamWriter(file))
                        {
                            sw.Write(sb.ToString());
                        }
                    }
                }
            }
        }
    }
}
