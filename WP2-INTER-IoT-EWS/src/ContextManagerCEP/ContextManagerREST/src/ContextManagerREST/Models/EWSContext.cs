using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Threading.Tasks;


namespace ContextManagerREST.Models
{
    public class EWSContext : DbContext
    {
        //private const string ConnectionStringName = "Server=tcp:mydriving.database.windows.net,1433;Initial Catalog=MyDrivingDB;Persist Security Info=False;User ID=mydrivingadmin;Password=1nter1otewsABC123;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"; // "Name=MS_TableConnectionString";
        //private const string ConnectionStringName = "Server=tcp:(localdb)\\MSSQLLocalDB;Initial Catalog=EWSContextDB_01;Integrated Security=True;"; 
        //private const string ConnectionStringName = "Server=tcp:(localdb)\\MSSQLLocalDB;AttachDbFilename=D:\\SQLDBs\\EWSContextDB_01.mdf;Integrated Security=SSPI;"; 
        private const string ConnectionStringName = "Server=tcp:(localdb)\\MSSQLLocalDB;Initial Catalog=EWSContextDB_01;AttachDbFilename=D:\\SQLDBs\\EWSContextDB_01.mdf;Integrated Security=SSPI;"; 

        public EWSContext() : base(ConnectionStringName)
        { }

        public System.Data.Entity.DbSet<ContextManager.DataObjects.SAREF.DeviceObservation> DeviceObservations { get; set; }

    }
}
