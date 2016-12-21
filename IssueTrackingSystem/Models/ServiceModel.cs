using BTS.Models;
using IssueTrackingSystem.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.ServiceModel;
using System.Web;
using System.Web.Security;

namespace ServiceClasses
{
    [ServiceContract]
    public interface IService
    {
        [OperationContract]
        List<Bug> GetProjectBugs(Project proj);

        [OperationContract]
        bool AddSolutionOfBug(int bugId, string solution);

        [OperationContract]
        bool SetDevIdForBug(int bugId, int id);

        [OperationContract]
        bool ReportBug(Bug b, BTS.Models.Attachment[] attachments);


        [OperationContract]
        bool RestartBug(int bugId);

        [OperationContract]
        bool SetBugStatus(int bugId, string status);

        [OperationContract]
        bool AddProject(Project proj, int[] categoryIds);

        [OperationContract]
        List<Project> GetProjectsByName(string name);

        [OperationContract]
        List<Project> GetProjectsByCategories(int[] categories, string lastId);

        [OperationContract]
        void ApproveDeveloperForProject(string projectName, int userId);

        [OperationContract]
        bool RemoveDevsFromProject(string projName, int[] toErase);

        [OperationContract]
        bool InviteDeveloperToProject(string projectName, int devId);

        [OperationContract]
        List<User> GetDevelopersOfProject(string projName, out User[] invitedDevelopers);

        [OperationContract]
        bool ConfirmUser(int userId);

        [OperationContract]
        string AddAccount(User u);

        [OperationContract]
        string getNickname(string email);

        [OperationContract]
        List<User> getUsers();

        [OperationContract]
        bool isEmailSent(string email);

        [OperationContract]
        List<User> searchForUsers(int id, string[] userNames, string[] userStatuses);

        [OperationContract]
        void InformAboutNotification(User u);

        [OperationContract]
        void WriteLetterToUser(User u, string subject, string text);

        [OperationContract]
        List<Notification> GetNotificationsOfUser(string receiver);

        [OperationContract]
        bool EditUserEmail(int id, string email);

        [OperationContract]
        bool EditUserBirthDate(int id, string birthdate);

        [OperationContract]
        bool ChangeUserPassword(string queryString, string password);

        [OperationContract]
        User getAuthenticatedUser(string nickname, string password);

        [OperationContract]
        bool EditUserAvatar(int id, byte[] avatar);

        [OperationContract]
        bool IsPasswordResetLinkValid(string queryString);

        [OperationContract]
        string[] GetBugAttachmentNames(int bugId);

        [OperationContract]
        bool RemoveNotification(int id);

        [OperationContract]
        void deleteExpiredRecords();

        [OperationContract]
        BTS.Models.Attachment GetBugAttachment(int bugId, string attachmentName);

        [OperationContract]
        void WriteMessage(string To, string From, string message);

        [OperationContract]
        bool MarkRightIssueAnswer(int bugId, int selectedItemId, int estimate);

        [OperationContract]
        void AddMessageToWorkflow(int bugId, Message message);

        [OperationContract]
        List<Category> getCategories();

        [OperationContract]
        List<Message> GetMessageLog(int bugId);
    }

    public class WebService : IService
    {
        // BUG FUNCTIONS

        public BTS.Models.Attachment GetBugAttachment(int bugId, string attachmentName)
        {
            BTS.Models.Attachment toReturn = null;

            string cmdString = "SELECT * FROM Attachments WHERE BUG_ID = @id AND NAME=@name";

            using (SqlConnection connection = new SqlConnection("data source=.\\SQLEXPRESS; database=BtsDB; integrated security=SSPI"))
            {
                connection.Open();

                SqlCommand cmd = new SqlCommand(cmdString, connection);
                cmd.Parameters.AddWithValue("@id", bugId);
                cmd.Parameters.AddWithValue("@name", attachmentName);

                SqlDataReader rdr = null;

                try
                {
                    rdr = cmd.ExecuteReader();

                    if(rdr.Read())
                    {
                        toReturn = new BTS.Models.Attachment();
                        toReturn.Name = rdr["NAME"].ToString();
                        toReturn.BugId = int.Parse(rdr["BUG_ID"].ToString());
                        toReturn.Data = (byte[])rdr["DATA"];
                    }
                }
                catch(Exception ex)
                {
                    ErrorTracker tracker = new ErrorTracker();
                    tracker.LogError(ex.ToString());
                    toReturn = null;
                }
                finally
                {
                    rdr.Close();
                }
            }

                return toReturn;
        }
        public bool AddSolutionOfBug(int bugId, string solution)
        {
            bool toReturn = false;
            string cmdString = "UPDATE Bugs SET Solution='" + solution + "' WHERE ID=" + bugId + ";";

            using (SqlConnection connection = new SqlConnection("data source=.\\SQLEXPRESS; database=BtsDB; integrated security=SSPI"))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(cmdString, connection);
                SqlTransaction transaction = connection.BeginTransaction("SolutionAddTransaction");
                cmd.Transaction = transaction;

                try
                {
                    cmd.ExecuteNonQuery();
                    transaction.Commit();
                    toReturn = true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    toReturn = false;

                    ErrorTracker tracker = new ErrorTracker();
                    tracker.LogError(ex.ToString());
                }
            }

            return toReturn;
        }

        public string[] GetBugAttachmentNames(int bugId)
        {

            List<string> returnList = new List<string>();

            string cmdString = "SELECT NAME FROM Attachments WHERE BUG_ID = @id";

            using (SqlConnection connection = new SqlConnection("data source=.\\SQLEXPRESS; database=BtsDB; integrated security=SSPI"))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(cmdString, connection);
                cmd.Parameters.AddWithValue("@id", bugId);

                SqlDataReader rdr = null;

                try
                {
                    rdr = cmd.ExecuteReader();

                    while(rdr.Read())
                    {
                        string name = rdr["NAME"].ToString();
                        returnList.Add(name);
                    }
                }
                catch (Exception ex)
                {
                    ErrorTracker tracker = new ErrorTracker();
                    tracker.LogError(ex.ToString());
                }
                finally
                {
                    rdr.Close();
                }
            }

            return returnList.ToArray();
        }

        public int GetBugId(string subject)
        {
            int toReturn = -1;
            string cmdString = "SELECT ID FROM Bugs WHERE SUBJECT='" + subject + "'";

            using (SqlConnection connection = new SqlConnection("data source=.\\SQLEXPRESS; database=BtsDB; integrated security=SSPI"))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(cmdString, connection);
                SqlTransaction transaction = connection.BeginTransaction("GetBugId");
                cmd.Transaction = transaction;
                SqlDataReader rdr = null;

                try
                {
                    rdr = cmd.ExecuteReader();

                    if (rdr.Read())
                    {
                        toReturn = Convert.ToInt32(rdr["ID"].ToString());
                    }
                    rdr.Close();
                    transaction.Commit();

                }
                catch (Exception ex)
                {
                    rdr.Close();
                    transaction.Rollback();

                    ErrorTracker tracker = new ErrorTracker();
                    tracker.LogError(ex.Message);
                }
            }

            return toReturn;
        }
        public List<Bug> GetProjectBugs(Project proj)
        {
            List<Bug> toReturn = new List<Bug>();

            string cmdString = "SELECT * FROM Bugs WHERE PROJECT_ID = '" + proj.Id + "';";

            using (SqlConnection connection = new SqlConnection("data source=.\\SQLEXPRESS; database=BtsDB; integrated security=SSPI"))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(cmdString, connection);

                SqlTransaction transaction = connection.BeginTransaction("BugsOfProject");

                cmd.Transaction = transaction;

                SqlDataReader rdr = null;

                try
                {
                    rdr = cmd.ExecuteReader();


                    while (rdr.Read())
                    {
                        Bug bug = new Bug();

                        bug.Id = Convert.ToInt32(rdr["ID"].ToString());
                        bug.Subject = rdr["SUBJECT"].ToString();
                        bug.Description = rdr["DESCRIPTION"].ToString();
                        bug.Status = rdr["STATUS"].ToString();
                        bug.TopicStarter = rdr["TOPIC_STARTER"].ToString();
                        bug.StatusChangeDate = Convert.ToDateTime(rdr["StatusChangeDate"].ToString());
                        bug.AddingTime = Convert.ToDateTime(rdr["AddingTime"].ToString());

                        if (rdr["DEVELOPER_ID"] != DBNull.Value)
                        {
                            bug.DeveloperId = Convert.ToInt32(rdr["DEVELOPER_ID"].ToString());
                        }

                        if (rdr["ESTIMATE"] != DBNull.Value)
                        {
                            bug.Estimate = Convert.ToInt32(rdr["ESTIMATE"].ToString());
                        }

                        if (rdr["Solution"] != DBNull.Value)
                        {
                            bug.Solution = rdr["Solution"].ToString();
                        }


                        toReturn.Add(bug);
                    }

                    rdr.Close();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    rdr.Close();
                    transaction.Rollback();

                    ErrorTracker tracker = new ErrorTracker();
                    tracker.LogError(ex.Message);
                }
            }

            if (toReturn != null && toReturn.Count() == 0)
            {
                toReturn = null;
            }
            return toReturn;
        }
        public bool ReportBug(Bug b, BTS.Models.Attachment[] attachments)
        {
            bool toReturn = false;

            using (SqlConnection connection = new SqlConnection("data source=.\\SQLEXPRESS; database=BtsDB; integrated security=SSPI"))
            {
                connection.Open();

                string cmdString = "INSERT INTO Bugs (SUBJECT, DESCRIPTION, PROJECT_ID, STATUS, TOPIC_STARTER, AddingTime, StatusChangeDate)"
                          + "VALUES(@Subject, @Description, @ProjectId, @Status, @TopicStarter, GETDATE(), GETDATE());";

                if(attachments != null)
                {
                    for(int i = 0; i < attachments.Count(); ++i)
                    {
                        cmdString += "INSERT INTO Attachments (NAME, DATA, BUG_ID) VALUES(@Name" + i + ", @Data" + i + ", IDENT_CURRENT('Bugs'));";
                    }
                }

                SqlCommand cmd = new SqlCommand(cmdString, connection);

                SqlParameter subject = new SqlParameter("@Subject", b.Subject);
                SqlParameter description = new SqlParameter("@Description", b.Description);
                SqlParameter projId = new SqlParameter("@ProjectId", b.ProjectId);
                SqlParameter status = new SqlParameter("@Status", b.Status);
                SqlParameter topicStarter = new SqlParameter("@TopicStarter", b.TopicStarter);

                cmd.Parameters.Add(subject);
                cmd.Parameters.Add(description);
                cmd.Parameters.Add(projId);
                cmd.Parameters.Add(status);
                cmd.Parameters.Add(topicStarter);

                if(attachments != null)
                {
                    for(int i = 0; i < attachments.Count(); ++i)
                    {
                        cmd.Parameters.AddWithValue("@Name" + i, attachments[i].Name);

                        SqlParameter paramData = new SqlParameter("@Data" + i, SqlDbType.Binary);
                        paramData.Value = attachments[i].Data;
                        cmd.Parameters.Add(paramData);
                    }
                }

                SqlTransaction transaction = connection.BeginTransaction("AddNewBug");
                cmd.Transaction = transaction;

                try
                {
                    cmd.ExecuteNonQuery();
                    transaction.Commit();
                    toReturn = true;
                }
                catch (Exception ex)
                {
                    toReturn = false;
                    transaction.Rollback();

                    ErrorTracker tracker = new ErrorTracker();
                    tracker.LogError(ex.Message);
                }
            }

            return toReturn;
        }

        public bool RestartBug(int bugId)
        {

            bool toReturn = false;

            string cmdString = "UPDATE Bugs SET STATUS = 'Assigned', Solution = NULL, StatusChangeDate=GETDATE(), ESTIMATE = NULL WHERE ID=@id; "
     + "UPDATE Messages SET CORRECT=0 WHERE BUG_ID = @id;";

            using (SqlConnection connection = new SqlConnection("data source=.\\SQLEXPRESS; database=BtsDB; integrated security=SSPI"))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(cmdString, connection);
                cmd.Parameters.AddWithValue("@id", bugId);

                SqlTransaction transaction = connection.BeginTransaction("MessageAddTransaction");
                cmd.Transaction = transaction;

                try
                {
                    cmd.ExecuteNonQuery();
                    transaction.Commit();

                    toReturn = true;
                }
                catch (Exception ex)
                {
                    toReturn = false;
                    transaction.Rollback();

                    ErrorTracker tracker = new ErrorTracker();
                    tracker.LogError(ex.ToString());
                }
            }

            return toReturn;
        }
        public bool SetBugStatus(int bugId, string status)
        {
            bool toReturn = false;

            string cmdString = "UPDATE Bugs SET STATUS=@Status, StatusChangeDate=GETDATE() WHERE ID= @id";

            using (SqlConnection connection = new SqlConnection("data source=.\\SQLEXPRESS; database=BtsDB; integrated security=SSPI"))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(cmdString, connection);
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.Parameters.AddWithValue("@id", bugId);

                SqlTransaction transaction = connection.BeginTransaction("BugStatusTransaction");
                cmd.Transaction = transaction;

                try
                {
                    cmd.ExecuteNonQuery();
                    transaction.Commit();
                    toReturn = true;
                }
                catch (Exception ex)
                {
                    toReturn = false;
                    transaction.Rollback();

                    ErrorTracker tracker = new ErrorTracker();
                    tracker.LogError(ex.ToString());
                }
            }

            return toReturn;
        }
        public bool SetDevIdForBug(int bugId, int id)
        {
            bool toReturn = false;

            string cmdString = "UPDATE Bugs SET DEVELOPER_ID=" + id + ", StatusChangeDate = GETDATE() WHERE ID=" + bugId + ";";

            using (SqlConnection connection = new SqlConnection("data source=.\\SQLEXPRESS; database=BtsDB; integrated security=SSPI"))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(cmdString, connection);
                SqlTransaction transaction = connection.BeginTransaction("AlterDevId");
                cmd.Transaction = transaction;

                try
                {
                    cmd.ExecuteNonQuery();
                    transaction.Commit();
                    toReturn = true;
                }
                catch
                {
                    toReturn = false;
                    transaction.Rollback();
                }
            }

            return toReturn;
        }

        // PROJECT FUNCTIONS

        public bool AddProject(Project proj, int[] categoryIds)
        {
            bool toReturn = false;

            string cmdString = "INSERT INTO Projects (NAME, DESCRIPTION, LOGO, PmId) VALUES(@Name, @Descr, @Logo, @PmId);";

            foreach (int categoryId in categoryIds)
            {
                cmdString += "INSERT INTO ProjectCategory VALUES('" + proj.Name + "', " + categoryId + ");";
            }

            using (SqlConnection connection = new SqlConnection("data source=.\\SQLEXPRESS; database=BtsDB; integrated security=SSPI"))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(cmdString, connection);
                SqlTransaction transaction = connection.BeginTransaction("ProjectAddTransaction");
                cmd.Transaction = transaction;

                cmd.Parameters.AddWithValue("@Name", proj.Name);
                cmd.Parameters.AddWithValue("@Descr", proj.Description);


                SqlParameter param = new SqlParameter("@Logo", SqlDbType.Binary);

                if (proj.Logo != null)
                {
                    param.Value = proj.Logo;
                }
                else
                {
                    param.Value = DBNull.Value;
                }

                cmd.Parameters.Add(param);

                cmd.Parameters.AddWithValue("@PmId", proj.PmId.ToString());

                try
                {
                    cmd.ExecuteNonQuery();
                    transaction.Commit();
                    toReturn = true;
                }
                catch (Exception ex)
                {
                    toReturn = false;
                    transaction.Rollback();

                    ErrorTracker tracker = new ErrorTracker();
                    tracker.LogError(ex.ToString());
                }
            }

            return toReturn;
        }
        public List<Project> GetProjectsByName(string name)
        {
            List<Project> toReturn = new List<Project>();

            if (name != null)
            {
                string cmdString = "SELECT * FROM Projects WHERE UPPER(NAME) LIKE '%" + name.ToUpper() + "%';";

                using (SqlConnection connection = new SqlConnection("data source=.\\SQLEXPRESS; database=BtsDB; integrated security=SSPI"))
                {
                    connection.Open();
                    SqlCommand cmd = new SqlCommand(cmdString, connection);

                    SqlTransaction transaction = connection.BeginTransaction("FindByTitle");
                    cmd.Transaction = transaction;
                    SqlDataReader reader = null;

                    try
                    {
                        reader = cmd.ExecuteReader();

                        while (reader.Read())
                        {
                            Project project = new Project();
                            project.Id = Convert.ToInt32(reader["ID"].ToString());
                            project.Description = reader["DESCRIPTION"].ToString();
                            project.Name = reader["NAME"].ToString();
                            project.PmId = int.Parse(reader["PmId"].ToString());

                            if (reader["LOGO"] != DBNull.Value)
                            {
                                project.Logo = (byte[])reader["LOGO"];
                            }

                            toReturn.Add(project);
                        }

                        reader.Close();
                        transaction.Commit();

                    }
                    catch (Exception ex)
                    {
                        reader.Close();
                        transaction.Rollback();

                        ErrorTracker tracker = new ErrorTracker();
                        tracker.LogError(ex.Message);
                    }
                }
            }

            toReturn = toReturn.GroupBy(x => x.Id).Select(group => group.First()).ToList();

            return toReturn;
        }
        public List<Project> GetProjectsByCategories(int[] categories, string lastId)
        {
            List<Project> toReturn = new List<Project>();


            if (categories != null && categories.Count() > 0)
            {
                string cmdString = "SELECT TOP 5 A.ID, A.NAME, A.DESCRIPTION, A.PmId, A.LOGO" +
                                    " FROM Projects A inner join ProjectCategory B on A.NAME = B.PROJECT_NAME" +
                                    " WHERE (";

                for (int i = 0; i < categories.Length; ++i)
                {
                    cmdString += "B.CATEGORY_ID = " + categories[i] + " OR ";
                }

                cmdString = cmdString.Substring(0, cmdString.Length - 4);

                if (lastId != null)
                {
                    cmdString += ") AND A.ID > " + lastId + ";";
                }
                else
                {
                    cmdString += ");";
                }


                using (SqlConnection connection = new SqlConnection("data source=.\\SQLEXPRESS; database=BtsDB; integrated security=SSPI"))
                {
                    connection.Open();
                    SqlCommand cmd = new SqlCommand(cmdString, connection);
                    SqlTransaction transaction = connection.BeginTransaction("Categories");
                    cmd.Transaction = transaction;

                    try
                    {
                        SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                        DataTable table = new DataTable();
                        adapter.Fill(table);

                        foreach (DataRow row in table.Rows)
                        {
                            Project project = new Project();
                            project.Id = Convert.ToInt32(row["ID"].ToString());
                            project.Description = row["DESCRIPTION"].ToString();
                            project.Name = row["NAME"].ToString();
                            project.PmId = int.Parse(row["PmId"].ToString());

                            if (row["LOGO"] != DBNull.Value)
                            {
                                project.Logo = (byte[])row["LOGO"];
                            }

                            toReturn.Add(project);
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();

                        ErrorTracker tracker = new ErrorTracker();
                        tracker.LogError(ex.Message);
                    }
                }
            }

            toReturn = toReturn.GroupBy(x => x.Id).Select(group => group.First()).ToList();

            return toReturn;
        }
        public void ApproveDeveloperForProject(string projectName, int userId)
        {
            string cmdString = "UPDATE ProjectDeveloper SET Approved=1 WHERE PROJ_NAME='" + projectName + "' AND DEV_ID=" + userId + ";";


            using (SqlConnection connection = new SqlConnection("data source=.\\SQLEXPRESS; database=BtsDB; integrated security=SSPI"))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(cmdString, connection);
                SqlTransaction transaction = connection.BeginTransaction("DevApproveTransaction");
                cmd.Transaction = transaction;

                try
                {
                    cmd.ExecuteNonQuery();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ErrorTracker tracker = new ErrorTracker();
                    tracker.LogError(ex.ToString());
                }
            }
        }
        public bool RemoveDevsFromProject(string projName, int[] toErase)
        {
            bool toReturn = false;

            string cmdString = "DELETE FROM ProjectDeveloper WHERE";

            foreach (int eraseId in toErase)
            {
                cmdString += " DEV_ID=" + eraseId + " OR";
            }

            cmdString = cmdString.Substring(0, cmdString.Length - 3);
            cmdString += ";";

            cmdString += " UPDATE Bugs SET DEVELOPER_ID=0 WHERE ";

            foreach (int eraseId in toErase)
            {
                cmdString += " DEVELOPER_ID=" + eraseId + " OR";
            }

            cmdString = cmdString.Substring(0, cmdString.Length - 3);
            cmdString += ";";


            using (SqlConnection connection = new SqlConnection("data source=.\\SQLEXPRESS; database=BtsDB; integrated security=SSPI"))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(cmdString, connection);
                SqlTransaction transaction = connection.BeginTransaction("EraseDevsTransaction");
                cmd.Transaction = transaction;

                try
                {
                    cmd.ExecuteNonQuery();
                    transaction.Commit();
                    toReturn = true;
                }
                catch (Exception ex)
                {
                    toReturn = false;
                    transaction.Rollback();
                    ErrorTracker tracker = new ErrorTracker();
                    tracker.LogError(ex.ToString());
                }
            }

            return toReturn;
        }
        public bool InviteDeveloperToProject(string projectName, int devId)
        {
            bool toReturn = false;

            string cmdString = "INSERT INTO ProjectDeveloper(PROJ_NAME, DEV_ID, Approved) VALUES('" + projectName + "', " + devId + ", 0);";


            using (SqlConnection connection = new SqlConnection("data source=.\\SQLEXPRESS; database=BtsDB; integrated security=SSPI"))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(cmdString, connection);
                SqlTransaction transaction = connection.BeginTransaction("InsertDevTransaction");
                cmd.Transaction = transaction;

                try
                {
                    cmd.ExecuteNonQuery();
                    transaction.Commit();
                    toReturn = true;
                }
                catch (Exception ex)
                {
                    toReturn = false;
                    transaction.Rollback();

                    ErrorTracker tracker = new ErrorTracker();
                    tracker.LogError(ex.ToString());
                }
            }

            return toReturn;
        }
        public List<User> GetDevelopersOfProject(string projName, out User[] invitedDevelopers)
        {
            List<User> toReturn = null;
            List<User> invitedDevs = new List<User>();

            string cmdString = "SELECT A.ID, A.NAME, A.SURNAME, A.NICKNAME, A.STATUS, A.Confirmed, B.Approved "
                + "FROM Users A inner join ProjectDeveloper B ON A.ID = B.DEV_ID "
                + "WHERE B.PROJ_NAME='" + projName + "';";

            using (SqlConnection connection = new SqlConnection("data source=.\\SQLEXPRESS; database=BtsDB; integrated security=SSPI"))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(cmdString, connection);
                SqlTransaction transaction = connection.BeginTransaction("ExtractProjectDevelopers");
                cmd.Transaction = transaction;
                SqlDataReader rdr = null;

                try
                {
                    rdr = cmd.ExecuteReader();

                    toReturn = new List<User>();

                    while (rdr.Read())
                    {
                        User u = new User();

                        u.Id = Convert.ToInt32(rdr["ID"].ToString());
                        u.Name = rdr["NAME"].ToString();
                        u.Surname = rdr["SURNAME"].ToString();
                        u.Nickname = rdr["NICKNAME"].ToString();
                        u.Status = rdr["STATUS"].ToString();

                        if (rdr["Confirmed"] != DBNull.Value)
                        {
                            u.Confirmed = Convert.ToBoolean(rdr["Confirmed"].ToString());
                        }
                        else
                        {
                            u.Confirmed = false;
                        }

                        if (u.Confirmed)
                        {
                            if (rdr["Approved"] != DBNull.Value)
                            {
                                if (Convert.ToBoolean(rdr["Approved"].ToString()))
                                {
                                    toReturn.Add(u);
                                }
                                else
                                {
                                    invitedDevs.Add(u);
                                }
                            }
                            else
                            {
                                invitedDevs.Add(u);
                            }
                        }
                    }

                    rdr.Close();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    rdr.Close();
                    transaction.Rollback();
                    ErrorTracker tracker = new ErrorTracker();
                    tracker.LogError(ex.ToString());
                }
            }

            invitedDevelopers = invitedDevs.ToArray();

            if (toReturn != null && toReturn.Count() == 0)
            {
                toReturn = null;
            }

            return toReturn;
        }

        // USER FUNCTIONS
        private string insertAccount(User u)
        {

            string passw = FormsAuthentication.HashPasswordForStoringInConfigFile(u.Password, "SHA1");

            string insertCmdString = "INSERT INTO Users(NAME, SURNAME, NICKNAME, BIRTHDATE, EMAIL, AVATAR, PASSWORD, STATUS, Confirmed) "
                        + " VALUES('" + u.Name + "', '" + u.Surname + "', '" + u.Nickname + "', CONVERT(smalldatetime,'" + u.BirthDate.ToString().Substring(0, 10) + "',104), '"
                        + u.Email + "',@Avatar,'" + passw + "', '" + u.Status + "', 0);";

            SqlParameter param = new SqlParameter("@Avatar", SqlDbType.Binary);

            if (u.Avatar != null)
            {
                param.Value = u.Avatar;
            }
            else
            {
                param.Value = DBNull.Value;
            }

            using (SqlConnection connection = new SqlConnection("data source=.\\SQLEXPRESS; database=BtsDB; integrated security=SSPI"))
            {
                connection.Open();
                SqlCommand insertCmd = new SqlCommand(insertCmdString, connection);
                SqlTransaction transaction2 = connection.BeginTransaction("SampleTransaction");
                insertCmd.Parameters.Add(param);

                insertCmd.Transaction = transaction2;

                try
                {
                    insertCmd.ExecuteNonQuery();
                    transaction2.Commit();

                    return "Success";
                }
                catch (Exception ex)
                {
                    transaction2.Rollback();

                    ErrorTracker tracker = new ErrorTracker();
                    tracker.LogError(ex.Message);

                    return "Fail";
                }
            }
        }
        public bool ConfirmUser(int userId)
        {
            bool toReturn = false;

            string cmdString = "UPDATE Users SET Confirmed=1 WHERE ID=" + userId + ";";

            using (SqlConnection connection = new SqlConnection("data source=.\\SQLEXPRESS; database=BtsDB; integrated security=SSPI"))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(cmdString, connection);
                SqlTransaction transaction = connection.BeginTransaction("UserConfirmTransaction");
                cmd.Transaction = transaction;

                try
                {
                    cmd.ExecuteNonQuery();
                    transaction.Commit();
                    toReturn = true;
                }
                catch (Exception ex)
                {
                    toReturn = false;
                    transaction.Rollback();

                    ErrorTracker tracker = new ErrorTracker();
                    tracker.LogError(ex.ToString());
                }
            }

            return toReturn;
        }
        public string AddAccount(User u)
        {
            string checkCmdString = "SELECT * FROM Users WHERE NICKNAME = '" + u.Nickname
                + "' OR EMAIL = '" + u.Email + "';";

            using (SqlConnection connection = new SqlConnection("data source=.\\SQLEXPRESS; database=BtsDB; integrated security=SSPI"))
            {
                connection.Open();
                SqlCommand checkCmd = new SqlCommand(checkCmdString, connection);
                SqlTransaction transaction1 = connection.BeginTransaction("SampleTransaction");
                SqlDataReader reader = null;

                checkCmd.Transaction = transaction1;

                try
                {
                    reader = checkCmd.ExecuteReader();

                    bool isOk = !(reader.HasRows);
                    reader.Close();
                    transaction1.Commit();

                    if (!isOk)
                    {
                        return "Duplicate";
                    }
                    else
                    {
                        string toReturn = insertAccount(u);

                        return toReturn;
                    }
                }
                catch (Exception ex)
                {
                    reader.Close();
                    transaction1.Rollback();

                    ErrorTracker tracker = new ErrorTracker();
                    tracker.LogError(ex.Message);
                    return "Fail";
                }
            }
        }
        public string getNickname(string email)
        {
            string toReturn = "";
            string cmdString = "SELECT * FROM Users WHERE EMAIL='" + email + "';";

            using (SqlConnection connection = new SqlConnection("data source=.\\SQLEXPRESS; database=BtsDB; integrated security=SSPI"))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand(cmdString, connection))
                {
                    SqlTransaction transaction = connection.BeginTransaction("PasswordRequest");
                    SqlDataReader reader = null;
                    cmd.Transaction = transaction;

                    try
                    {
                        reader = cmd.ExecuteReader();

                        if (reader.Read())
                        {
                            toReturn = reader["NICKNAME"].ToString();
                        }

                        reader.Close();
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        reader.Close();
                        transaction.Rollback();

                        ErrorTracker tracker = new ErrorTracker();
                        tracker.LogError(ex.Message);
                    }
                }
            }

            return toReturn;
        }
        public List<User> getUsers()
        {
            List<User> toReturn = new List<User>();

            string cmdString = "SELECT * FROM Users;";

            using (SqlConnection connection = new SqlConnection("data source=.\\SQLEXPRESS; database=BtsDB; integrated security=SSPI"))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand(cmdString, connection))
                {
                    SqlTransaction transaction = connection.BeginTransaction("ExtractUsers");
                    cmd.Transaction = transaction;
                    SqlDataReader reader = null;

                    try
                    {

                        reader = cmd.ExecuteReader();

                        while (reader.Read())
                        {
                            User user = new User();

                            user.Id = Convert.ToInt32(reader["ID"].ToString());
                            user.Name = reader["NAME"].ToString();
                            user.Surname = reader["SURNAME"].ToString();
                            user.Nickname = reader["NICKNAME"].ToString();
                            user.BirthDate = Convert.ToDateTime(reader["BIRTHDATE"].ToString());
                            user.Password = reader["PASSWORD"].ToString();
                            user.Status = reader["STATUS"].ToString();
                            user.Email = reader["EMAIL"].ToString();

                            if (reader["Confirmed"] != DBNull.Value)
                            {
                                user.Confirmed = Convert.ToBoolean(reader["Confirmed"].ToString());
                            }
                            else user.Confirmed = false;

                            if (reader["AVATAR"] != DBNull.Value)
                            {
                                user.Avatar = (byte[])(reader["AVATAR"]);
                            }
                            else
                            {
                                user.Avatar = null;
                            }

                            toReturn.Add(user);
                        }

                        reader.Close();
                        transaction.Commit();

                    }
                    catch (Exception ex)
                    {
                        reader.Close();
                        transaction.Rollback();

                        ErrorTracker tracker = new ErrorTracker();
                        tracker.LogError(ex.Message);
                    }
                }
            }

            return toReturn;
        }
        public bool isEmailSent(string email)
        {
            bool toReturn = false;


            using (SqlConnection connection = new SqlConnection("data source=.\\SQLEXPRESS; database=BtsDB; integrated security=SSPI"))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand("ResetPassword", connection);
                cmd.CommandType = CommandType.StoredProcedure;

                string nickname = getNickname(email);

                SqlParameter paramUsername = new SqlParameter("@UserName", nickname);

                cmd.Parameters.Add(paramUsername);

                SqlTransaction transaction = connection.BeginTransaction("PasswordResetTransaction");
                cmd.Transaction = transaction;
                SqlDataReader rdr = null;

                try
                {
                    rdr = cmd.ExecuteReader();

                    while (rdr.Read())
                    {
                        if (Convert.ToBoolean(rdr["ReturnCode"]))
                        {
                            toReturn = SendPasswordResetLetter(rdr["Email"].ToString(), nickname, rdr["UniqueId"].ToString());
                        }
                    }

                    rdr.Close();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    rdr.Close();
                    transaction.Rollback();

                    ErrorTracker tracker = new ErrorTracker();
                    tracker.LogError(ex.ToString());
                }
            }

            return toReturn;
        }
        public List<User> searchForUsers(int id, string[] userNames, string[] userStatuses)
        {
            List<User> toReturn = null;
            string cmdString = null;

            if (userNames.Count() == 0 && userStatuses == null)
            {
                cmdString = "";
            }
            else if (userNames.Count() == 0 && userStatuses != null)
            {
                cmdString = "SELECT TOP 5 * FROM Users WHERE ID>" + id + " AND(";

                foreach (string status in userStatuses)
                {
                    cmdString += "STATUS='" + status + "' OR ";
                }

                cmdString = cmdString.Substring(0, cmdString.Length - 4);
                cmdString += ");";
            }
            else if (userNames.Count() != 0 && userStatuses == null)
            {
                cmdString = "SELECT TOP 5 * FROM Users WHERE ID>" + id + " AND(";

                foreach (string name in userNames)
                {
                    cmdString += "(Upper(NAME) LIKE '%" + name.ToUpper() + "%' OR Upper(SURNAME) LIKE '%" + name.ToUpper() + "%' OR Upper(NICKNAME) LIKE '%" + name.ToUpper() + "%') OR ";
                }

                cmdString = cmdString.Substring(0, cmdString.Length - 4);
                cmdString += ");";
            }
            else
            {
                cmdString = "SELECT TOP 5 * FROM Users WHERE ID>" + id + " AND ((";

                foreach (string name in userNames)
                {
                    cmdString += "(Upper(NAME) LIKE '%" + name.ToUpper() + "%' OR Upper(SURNAME) LIKE '%" + name.ToUpper() + "%' OR Upper(NICKNAME) LIKE '%" + name.ToUpper() + "%') OR ";
                }

                cmdString = cmdString.Substring(0, cmdString.Length - 4);
                cmdString += ") AND (";

                foreach (string status in userStatuses)
                {
                    cmdString += "STATUS='" + status + "' OR ";
                }

                cmdString = cmdString.Substring(0, cmdString.Length - 4);

                cmdString += "));";
            }

            using (SqlConnection connection = new SqlConnection("data source=.\\SQLEXPRESS; database=BtsDB; integrated security=SSPI"))
            {
                connection.Open();
                if (cmdString != "")
                {
                    SqlCommand cmd = new SqlCommand(cmdString, connection);
                    SqlTransaction transaction = connection.BeginTransaction("UserSearch");
                    cmd.Transaction = transaction;
                    SqlDataReader reader = null;

                    try
                    {
                        reader = cmd.ExecuteReader();

                        toReturn = new List<User>();

                        while (reader.Read())
                        {
                            User user = new User();

                            user.Id = Convert.ToInt32(reader["ID"].ToString());
                            user.Name = reader["NAME"].ToString();
                            user.Surname = reader["SURNAME"].ToString();
                            user.Nickname = reader["NICKNAME"].ToString();
                            user.BirthDate = Convert.ToDateTime(reader["BIRTHDATE"].ToString());
                            user.Password = reader["PASSWORD"].ToString();
                            user.Status = reader["STATUS"].ToString();
                            user.Email = reader["EMAIL"].ToString();

                            if (reader["Confirmed"] != DBNull.Value)
                            {
                                user.Confirmed = Convert.ToBoolean(reader["Confirmed"].ToString());
                            }
                            else user.Confirmed = false;

                            if (reader["AVATAR"] != DBNull.Value)
                            {
                                user.Avatar = (byte[])(reader["AVATAR"]);
                            }
                            else
                            {
                                user.Avatar = null;
                            }

                            toReturn.Add(user);
                        }

                        reader.Close();
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        reader.Close();
                        transaction.Rollback();
                        ErrorTracker tracker = new ErrorTracker();
                        tracker.LogError(ex.Message);
                    }
                }
            }

            if (toReturn != null && toReturn.Count() == 0)
            {
                toReturn = null;
            }

            return toReturn;
        }
        public void InformAboutNotification(User u)
        {
            string cmdString = "SELECT COUNT(ID) FROM Notifications WHERE RECEIVER=@receiver";

            using (SqlConnection connection = new SqlConnection("data source=.\\SQLEXPRESS; database=BtsDB; integrated security=SSPI"))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(cmdString, connection);
                cmd.Parameters.AddWithValue("@receiver", u.Nickname);

                int numOfNotifications = 0;

                SqlTransaction transaction = connection.BeginTransaction("InformTransaction");
                cmd.Transaction = transaction;

                try
                {
                    numOfNotifications = (int)cmd.ExecuteScalar();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    ErrorTracker tracker = new ErrorTracker();
                    tracker.LogError(ex.ToString());
                }

                if (numOfNotifications == 1)
                {
                    string subject = "New message";
                    string text = "Hello, " + u.Name + ", <br/> You received new message on your account. <br/>"
                        + "Follow this link to check current notifications: http://its.local <br/> Issue Tracking System";
                    WriteLetterToUser(u, subject, text);
                }
            }
        }
        public void WriteLetterToUser(User u, string subject, string text)
        {
            string smtpHost = "smtp.gmail.com";
            int smtpPort = 587;
            string smtpUserName = "btsemail1@gmail.com";
            string smtpUserPass = "btsadmin";

            SmtpClient client = new SmtpClient(smtpHost, smtpPort);
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(smtpUserName, smtpUserPass);
            client.EnableSsl = true;

            string msgFrom = smtpUserName;
            string msgTo = u.Email;
            string msgSubject = "Password Notification";

            MailMessage message = new MailMessage(msgFrom, msgTo, msgSubject, text);

            message.IsBodyHtml = true;

            try
            {
                client.Send(message);
            }
            catch (Exception ex)
            {
                ErrorTracker tracker = new ErrorTracker();
                tracker.LogError(ex.Message);
            }
        }
        public List<Notification> GetNotificationsOfUser(string receiver)
        {
            List<Notification> toReturn = null;

            string cmdString = "SELECT * FROM Notifications WHERE RECEIVER='" + receiver + "';";

            using (SqlConnection connection = new SqlConnection("data source=.\\SQLEXPRESS; database=BtsDB; integrated security=SSPI"))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(cmdString, connection);
                SqlTransaction transaction = connection.BeginTransaction("UserNotificationsTransaction");
                cmd.Transaction = transaction;
                SqlDataReader rdr = null;

                try
                {
                    rdr = cmd.ExecuteReader();

                    toReturn = new List<Notification>();

                    while (rdr.Read())
                    {
                        Notification n = new Notification();

                        n.Sender = rdr["SENDER"].ToString();
                        n.Receiver = rdr["RECEIVER"].ToString();
                        n.SendDate = Convert.ToDateTime(rdr["SEND_TIME"].ToString());
                        n.Message = rdr["MESSAGE"].ToString();
                        n.Id = int.Parse(rdr["ID"].ToString());

                        toReturn.Add(n);
                    }

                    rdr.Close();
                    transaction.Commit();

                }
                catch (Exception ex)
                {
                    rdr.Close();
                    transaction.Rollback();
                    ErrorTracker tracker = new ErrorTracker();
                    tracker.LogError(ex.ToString());
                }
            }

            if (toReturn != null && toReturn.Count() == 0)
            {
                toReturn = null;
            }
            else
            {
                toReturn = toReturn.OrderByDescending(x => x.SendDate).ToList();
            }

            return toReturn;
        }
        public bool EditUserEmail(int id, string email)
        {
            bool toReturn = false;

            string cmdString = "UPDATE Users SET EMAIL='" + email + "' WHERE ID=" + id + ";";

            using (SqlConnection connection = new SqlConnection("data source=.\\SQLEXPRESS; database=BtsDB; integrated security=SSPI"))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(cmdString, connection);
                SqlTransaction transaction = connection.BeginTransaction("EditEmail");
                cmd.Transaction = transaction;

                try
                {
                    cmd.ExecuteNonQuery();
                    transaction.Commit();
                    toReturn = true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    ErrorTracker tracker = new ErrorTracker();
                    tracker.LogError(ex.Message);

                }
            }

            return toReturn;
        }
        public bool EditUserBirthDate(int id, string birthdate)
        {
            bool toReturn = false;

            string cmdString = "UPDATE Users SET BIRTHDATE=CONVERT(smalldatetime, '" + birthdate + "', 104) WHERE ID=" + id + ";";

            using (SqlConnection connection = new SqlConnection("data source=.\\SQLEXPRESS; database=BtsDB; integrated security=SSPI"))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(cmdString, connection);
                SqlTransaction transaction = connection.BeginTransaction("EditBirthDate");
                cmd.Transaction = transaction;

                try
                {
                    cmd.ExecuteNonQuery();
                    transaction.Commit();
                    toReturn = true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    ErrorTracker tracker = new ErrorTracker();
                    tracker.LogError(ex.Message);
                }
            }

            return toReturn;
        }
        public bool ChangeUserPassword(string queryString, string password)
        {
            List<SqlParameter> paramList = new List<SqlParameter>()
               {
                 new SqlParameter()
                    {
                         ParameterName = "@GUID",
                         Value = queryString
                    },
                new SqlParameter()
                     {
                         ParameterName = "@Password",
                         Value = FormsAuthentication.HashPasswordForStoringInConfigFile(password, "SHA1")
                     }
                };

            return ExecuteSP("ChangePassword", paramList);
        }
        private bool SendPasswordResetLetter(string toMail, string username, string uniqueid)
        {
            string smtpHost = "smtp.gmail.com";
            int smtpPort = 587;
            string smtpUserName = "btsemail1@gmail.com";
            string smtpUserPass = "btsadmin";

            SmtpClient client = new SmtpClient(smtpHost, smtpPort);
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(smtpUserName, smtpUserPass);
            client.EnableSsl = true;

            string msgFrom = smtpUserName;
            string msgTo = toMail;
            string msgSubject = "Password Notification";

            string msgBody = "Dear " + username + ", <br/><br/>";
            msgBody += "Please follow this link: http://its.local/Bts/ChangePassword?uid=" + uniqueid + " to change your password";
            MailMessage message = new MailMessage(msgFrom, msgTo, msgSubject, msgBody);

            message.IsBodyHtml = true;

            try
            {
                client.Send(message);
                return true;
            }
            catch (Exception ex)
            {
                ErrorTracker tracker = new ErrorTracker();
                tracker.LogError(ex.Message);

                return false;
            }
        }
        public User getAuthenticatedUser(string nickname, string password)
        {
            User toReturn = new User();
            toReturn.Id = -1;
            SqlDataReader reader = null;

            string encryptedPassword = FormsAuthentication.HashPasswordForStoringInConfigFile(password, "SHA1");

            string cmdString = "SELECT * FROM Users WHERE NICKNAME='" + nickname +
                "' AND PASSWORD='" + encryptedPassword + "';";

            using (SqlConnection connection = new SqlConnection("data source=.\\SQLEXPRESS; database=BtsDB; integrated security=SSPI"))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand(cmdString, connection))
                {
                    SqlTransaction transaction = connection.BeginTransaction("FindUserTransaction");
                    cmd.Transaction = transaction;

                    try
                    {
                        reader = cmd.ExecuteReader();

                        if (reader.Read())
                        {
                            toReturn.Id = Convert.ToInt32(reader["ID"].ToString());
                            toReturn.Status = reader["STATUS"].ToString();

                            if (reader["Confirmed"] != DBNull.Value)
                            {
                                toReturn.Confirmed = Convert.ToBoolean(reader["Confirmed"].ToString());
                            }
                            else toReturn.Confirmed = false;
                        }

                        reader.Close();
                        transaction.Commit();

                    }
                    catch (Exception ex)
                    {
                        reader.Close();
                        transaction.Rollback();

                        ErrorTracker tracker = new ErrorTracker();
                        tracker.LogError(ex.Message);
                    }
                }
            }

            return toReturn;
        }
        private bool ExecuteSP(string SPName, List<SqlParameter> SPParameters)
        {

            bool toReturn = false;

            using (SqlConnection connection = new SqlConnection("data source=.\\SQLEXPRESS; database=BtsDB; integrated security=SSPI"))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(SPName, connection);
                cmd.CommandType = CommandType.StoredProcedure;

                foreach (SqlParameter parameter in SPParameters)
                {
                    cmd.Parameters.Add(parameter);
                }

                SqlTransaction transaction = connection.BeginTransaction("ExecuteCommand");
                cmd.Transaction = transaction;

                try
                {
                    toReturn = Convert.ToBoolean(cmd.ExecuteScalar());
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    ErrorTracker tracker = new ErrorTracker();
                    tracker.LogError(ex.Message);
                    toReturn = false;
                }
            }

            return toReturn;

        }
        public bool EditUserAvatar(int id, byte[] avatar)
        {
            bool toReturn = false;

            string cmdString = "UPDATE Users SET AVATAR=@Avatar WHERE ID=" + id + ";";

            SqlParameter param = new SqlParameter("@Avatar", SqlDbType.Binary);

            if (avatar != null)
            {
                param.Value = avatar;
            }
            else
            {
                param.Value = DBNull.Value;
            }

            using (SqlConnection connection = new SqlConnection("data source=.\\SQLEXPRESS; database=BtsDB; integrated security=SSPI"))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(cmdString, connection);
                cmd.Parameters.Add(param);
                SqlTransaction transaction = connection.BeginTransaction("EditAvatar");
                cmd.Transaction = transaction;

                try
                {
                    cmd.ExecuteNonQuery();
                    transaction.Commit();

                    toReturn = true;
                }
                catch
                {
                    transaction.Rollback();
                }
            }

            return toReturn;
        }
        public bool IsPasswordResetLinkValid(string queryString)
        {
            List<SqlParameter> paramList = new List<SqlParameter>()
              {
                    new SqlParameter()
                 {
                    ParameterName = "@GUID",
                    Value = queryString
                 }
              };

            return ExecuteSP("IsResetLinkValid", paramList);
        }


        public bool RemoveNotification(int id)
        {
            bool toReturn = false;

            string cmdString = "DELETE FROM Notifications WHERE ID=@id;";

            using (SqlConnection connection = new SqlConnection("data source=.\\SQLEXPRESS; database=BtsDB; integrated security=SSPI"))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(cmdString, connection);
                cmd.Parameters.AddWithValue("@id", id);
                SqlTransaction transaction = connection.BeginTransaction("NotificationRemoveTransaction");
                cmd.Transaction = transaction;

                try
                {
                    cmd.ExecuteNonQuery();
                    transaction.Commit();
                    toReturn = true;
                }
                catch (Exception ex)
                {
                    toReturn = false;
                    transaction.Rollback();

                    ErrorTracker tracker = new ErrorTracker();
                    tracker.LogError(ex.ToString());
                }
            }

            return toReturn;
        }
        public void deleteExpiredRecords()
        {
            using (SqlConnection connection = new SqlConnection("data source=.\\SQLEXPRESS; database=BtsDB; integrated security=SSPI"))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand("DeleteExpiredRecords", connection);
                cmd.CommandType = CommandType.StoredProcedure;

                SqlTransaction transaction = connection.BeginTransaction("ExpiredRecordsDeleting");

                cmd.Transaction = transaction;

                try
                {
                    cmd.ExecuteNonQuery();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    ErrorTracker tracker = new ErrorTracker();
                    tracker.LogError(ex.Message);
                }
            }
        }
        public void WriteMessage(string To, string From, string message)
        {
            string cmdString = "INSERT INTO Notifications(RECEIVER, SENDER, MESSAGE, SEND_TIME) VALUES('" + To + "', '" + From + "','"
             + message + "', CONVERT(smalldatetime, GETDATE(), 104));";

            using (SqlConnection connection = new SqlConnection("data source=.\\SQLEXPRESS; database=BtsDB; integrated security=SSPI"))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(cmdString, connection);
                SqlTransaction transaction = connection.BeginTransaction("NotificationAddTransaction");
                cmd.Transaction = transaction;

                try
                {
                    cmd.ExecuteNonQuery();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    ErrorTracker tracker = new ErrorTracker();
                    tracker.LogError(ex.ToString());
                }
            }
        }
        public void AddMessageToWorkflow(int bugId, Message message)
        {
            string cmdString = "INSERT INTO Messages (MESSAGE, ADD_TIME, SENDER_NICKNAME, BUG_ID, CORRECT, UserToReply, MessageIdToReply) VALUES('"
                + message.MessageText + "'," + "CONVERT(smalldatetime, GETDATE(), 104), '" + message.SenderNick + "', " + bugId + ", 0,'"
                + message.UserToReply + "', " + message.MessageToReplyId + ");";

            using (SqlConnection connection = new SqlConnection("data source=.\\SQLEXPRESS; database=BtsDB; integrated security=SSPI"))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(cmdString, connection);
                SqlTransaction transaction = connection.BeginTransaction("MessageAddTransaction");
                cmd.Transaction = transaction;

                try
                {
                    cmd.ExecuteNonQuery();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    ErrorTracker tracker = new ErrorTracker();
                    tracker.LogError(ex.ToString());
                }
            }
        }
        public bool MarkRightIssueAnswer(int bugId, int selectedItemId, int estimate)
        {
            bool toReturn = false;

            string cmdString = "UPDATE Messages SET CORRECT=1 WHERE ID=" + selectedItemId +
                "; UPDATE Bugs SET ESTIMATE=" + estimate + ", STATUS='Closed' WHERE ID=" + bugId + ";";

            using (SqlConnection connection = new SqlConnection("data source=.\\SQLEXPRESS; database=BtsDB; integrated security=SSPI"))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(cmdString, connection);
                SqlTransaction transaction = connection.BeginTransaction("RightAnswerTransaction");
                cmd.Transaction = transaction;

                try
                {
                    cmd.ExecuteNonQuery();
                    transaction.Commit();
                    toReturn = true;
                }
                catch (Exception ex)
                {
                    toReturn = false;
                    transaction.Rollback();

                    ErrorTracker tracker = new ErrorTracker();
                    tracker.LogError(ex.ToString());
                }
            }
            return toReturn;
        }
        public List<Category> getCategories()
        {
            List<Category> toReturn = new List<Category>();

            string cmdString = "SELECT * FROM Categories;";

            using (SqlConnection connection = new SqlConnection("data source=.\\SQLEXPRESS; database=BtsDB; integrated security=SSPI"))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(cmdString, connection);
                SqlDataReader rdr = null;

                SqlTransaction transaction = connection.BeginTransaction("CategoriesTrasaction");
                cmd.Transaction = transaction;

                try
                {
                    rdr = cmd.ExecuteReader();

                    while (rdr.Read())
                    {
                        Category category = new Category();
                        category.Title = rdr["TITLE"].ToString();
                        category.Id = Convert.ToInt32(rdr["ID"].ToString());
                        toReturn.Add(category);
                    }

                    rdr.Close();
                    transaction.Commit();

                }
                catch (Exception ex)
                {
                    rdr.Close();
                    transaction.Rollback();

                    ErrorTracker tracker = new ErrorTracker();
                    tracker.LogError(ex.Message);
                }
            }

            return toReturn;
        }
        public List<Message> GetMessageLog(int bugId)
        {
            List<Message> toReturn = null;

            string cmdString = "SELECT * FROM Messages WHERE BUG_ID=" + bugId + ";";

            using (SqlConnection connection = new SqlConnection("data source=.\\SQLEXPRESS; database=BtsDB; integrated security=SSPI"))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(cmdString, connection);
                SqlTransaction transaction = connection.BeginTransaction("ExtractMessages");
                cmd.Transaction = transaction;
                SqlDataReader rdr = null;

                try
                {
                    rdr = cmd.ExecuteReader();

                    toReturn = new List<Message>();

                    while (rdr.Read())
                    {
                        Message toAdd = new Message();

                        toAdd.Id = Convert.ToInt32(rdr["ID"].ToString());
                        toAdd.MessageText = rdr["MESSAGE"].ToString();
                        toAdd.SenderNick = rdr["SENDER_NICKNAME"].ToString();
                        toAdd.AddingTime = Convert.ToDateTime(rdr["ADD_TIME"].ToString());
                        toAdd.BugId = Convert.ToInt32(rdr["BUG_ID"].ToString());
                        toAdd.Correct = Convert.ToBoolean(rdr["CORRECT"].ToString());

                        toAdd.MessageText.Replace('_', ' ');

                        if (rdr["UserToReply"] != DBNull.Value)
                        {
                            toAdd.UserToReply = rdr["UserToReply"].ToString();
                        }

                        if (rdr["MessageIdToReply"] != DBNull.Value)
                        {
                            toAdd.MessageToReplyId = Convert.ToInt32(rdr["MessageIdToReply"].ToString());
                        }

                        toReturn.Add(toAdd);
                    }

                    rdr.Close();
                    transaction.Commit();

                }
                catch (Exception ex)
                {
                    rdr.Close();
                    transaction.Rollback();
                    ErrorTracker tracker = new ErrorTracker();
                    tracker.LogError(ex.ToString());
                }
            }

            if (toReturn != null && toReturn.Count == 0)
            {
                toReturn = null;
            }

            return toReturn;
        }
    }
}