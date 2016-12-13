using BTS.Models;
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using IssueTrackingSystem.Common;
using System.Web.Security;
using System.IO;
using System.Text;
using System.ServiceModel;
using ServiceClasses;
using System.Data.SqlClient;
using System.Data;
using System.Net.Mail;
using System.Net;

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
        bool ReportBug(Bug b);


        [OperationContract]
        bool RestartBug(int bugId);

        [OperationContract]
        bool SetBugStatus(int bugId, string status);

        [OperationContract]
        bool AddProject(Project proj, List<int> categoryIds);

        [OperationContract]
        List<Project> GetProjectsByName(string name);

        [OperationContract]
        List<Project> GetProjectsByCategories(int[] categories);

        [OperationContract]
        void ApproveDeveloperForProject(string projectName, int userId);

        [OperationContract]
        bool RemoveDevsFromProject(string projName, List<int> toErase);

        [OperationContract]
        bool InviteDeveloperToProject(string projectName, int devId);

        [OperationContract]
        List<User> GetDevelopersOfProject(string projName, out List<User> invitedDevelopers);

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
        List<User> searchForUsers(int id, List<string> userNames, List<string> userStatuses);

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
        bool AddAttachments(List<HttpPostedFileBase> attachments, int id);

        [OperationContract]
        bool RemoveNotification(int id);

        [OperationContract]
        void deleteExpiredRecords();

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
        public bool ReportBug(Bug b)
        {
            bool toReturn = false;

            using (SqlConnection connection = new SqlConnection("data source=.\\SQLEXPRESS; database=BtsDB; integrated security=SSPI"))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand("addBug", connection);
                cmd.CommandType = CommandType.StoredProcedure;

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

                SqlTransaction transaction = connection.BeginTransaction("AddNewBug");
                cmd.Transaction = transaction;

                try
                {
                    if ((int)cmd.ExecuteScalar() == 1)
                    {
                        toReturn = true;
                    }

                    transaction.Commit();
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

            string cmdString = "UPDATE Bugs SET STATUS = 'Assigned', StatusChangeDate=GETDATE(), ESTIMATE = NULL WHERE ID=@id; "
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

        public bool AddProject(Project proj, List<int> categoryIds)
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
        public List<Project> GetProjectsByCategories(int[] categories)
        {
            List<Project> toReturn = new List<Project>();


            if (categories != null)
            {
                string cmdString = "SELECT TOP 5 A.ID, A.NAME, A.DESCRIPTION, A.PmId, A.LOGO" +
                                    " FROM Projects A inner join ProjectCategory B on A.NAME = B.PROJECT_NAME" +
                                    " WHERE A.ID > " + categories[categories.Length - 1] + " AND ( ";

                for (int i = 0; i < categories.Length - 1; ++i)
                {
                    cmdString += "B.CATEGORY_ID = " + categories[i] + " OR ";
                }

                cmdString = cmdString.Substring(0, cmdString.Length - 4);

                cmdString += ");";

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
        public bool RemoveDevsFromProject(string projName, List<int> toErase)
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
        public List<User> GetDevelopersOfProject(string projName, out List<User> invitedDevelopers)
        {
            List<User> toReturn = null;
            invitedDevelopers = new List<User>();

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
                                    invitedDevelopers.Add(u);
                                }
                            }
                            else
                            {
                                invitedDevelopers.Add(u);
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
        public List<User> searchForUsers(int id, List<string> userNames, List<string> userStatuses)
        {
            List<User> toReturn = null;
            string cmdString = null;

            if (userNames.Count == 0 && userStatuses == null)
            {
                cmdString = "";
            }
            else if (userNames.Count == 0 && userStatuses != null)
            {
                cmdString = "SELECT TOP 5 * FROM Users WHERE ID>" + id + " AND(";

                foreach (string status in userStatuses)
                {
                    cmdString += "STATUS='" + status + "' OR ";
                }

                cmdString = cmdString.Substring(0, cmdString.Length - 4);
                cmdString += ");";
            }
            else if (userNames.Count != 0 && userStatuses == null)
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

        // OTHER FUNCTIONS

        public bool AddAttachments(List<HttpPostedFileBase> attachments, int id)
        {
            if (id != -1)
            {
                bool allSucceeded = true;
                using (SqlConnection connection = new SqlConnection("data source=.\\SQLEXPRESS; database=BtsDB; integrated security=SSPI"))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction("InsertAttachments");

                    foreach (HttpPostedFileBase attachment in attachments)
                    {
                        if (attachment != null)
                        {
                            string cmdString = "INSERT INTO Attachments (NAME, DATA, BUG_ID) VALUES(@Name, @Data, @BugId)";
                            SqlCommand cmd = new SqlCommand(cmdString, connection);
                            cmd.Transaction = transaction;

                            byte[] file = new byte[attachment.ContentLength];
                            attachment.InputStream.Read(file, 0, attachment.ContentLength);

                            SqlParameter paramName = new SqlParameter("@Name", attachment.GetType().ToString());
                            SqlParameter paramBugId = new SqlParameter("@BugId", id);
                            SqlParameter paramData = new SqlParameter("@Data", SqlDbType.Binary);
                            paramData.Value = file;

                            cmd.Parameters.Add(paramName);
                            cmd.Parameters.Add(paramData);
                            cmd.Parameters.Add(paramBugId);

                            try
                            {
                                cmd.ExecuteNonQuery();
                            }
                            catch (Exception ex)
                            {
                                allSucceeded = false;

                                ErrorTracker tracker = new ErrorTracker();
                                tracker.LogError(ex.Message);
                                break;
                            }
                        }
                    }

                    if (allSucceeded)
                    {
                        transaction.Commit();
                    }
                    else
                    {
                        transaction.Rollback();
                    }
                }

                        return allSucceeded;
                }
            else
            {
                    return false;
                }
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

    namespace BTS.Controllers
{
    [LogError]
    public class BtsController : Controller
    {
        private static Uri tcpUri = new Uri("http://localhost:1050/TestService");
        private static EndpointAddress address = new EndpointAddress(tcpUri);
        private static BasicHttpBinding binding = new BasicHttpBinding();
        

        private static ChannelFactory<IService> factory = new ChannelFactory<IService>(binding, address);
        private IService service = factory.CreateChannel();
        // GET: Bts

        public ActionResult Index(string message, string messageType)
        {
            binding.MaxReceivedMessageSize = Int32.MaxValue;

            if (Session["Username"] != null && Session["Status"] != null)
            {
                return RedirectToAction("Main", "Bts", new { message = message, messageType = messageType });
            }
            else
            {
                if (Request.Cookies["username"] != null && Request.Cookies["password"] != null)
                {
                        User u = service.getAuthenticatedUser(Request.Cookies["username"].Value, Request.Cookies["password"].Value);

                        if (u.Id != -1)
                        {
                            Session["Username"] = Request.Cookies["username"].Value;
                            Session["Status"] = u.Status;
                            Session["Confirmed"] = u.Confirmed.ToString();
                            Session["Id"] = u.Id;

                            return RedirectToAction("Main");
                        }
                    }

                if(message != null && messageType != null)
                {
                    TempData["message"] = message;
                    TempData["status"] = messageType;
                }

                return View();
            }
        }

        [HttpPost]
        public ActionResult ConfirmUser(int userId)
        {
            bool toReturn = false;

                toReturn = service.ConfirmUser(userId);


            return RedirectToAction("ExternalAccountPage", "Bts", new { id = userId });
        }

        public ActionResult LogIn(string Login, string Password, bool rememberMe)
        {

            bool authenticated = false;

                User u = service.getAuthenticatedUser(Login, Password);
                if (u.Id != -1)
                {
                    Session["Username"] = Login;
                    Session["Status"] = u.Status;
                    Session["Id"] = u.Id;
                    Session["Confirmed"] = u.Confirmed.ToString();

                    if (rememberMe)
                    {
                        HttpCookie nick = new HttpCookie("username");
                        nick.Expires = DateTime.Now.AddDays(1d);
                        nick.Value = Login;
                        Response.SetCookie(nick);

                        HttpCookie pass = new HttpCookie("password");
                        pass.Expires = DateTime.Now.AddDays(1d);
                        pass.Value = Password;
                        Response.SetCookie(pass);
                    }

                    authenticated = true;
                }


            if (!authenticated)
            {
                TempData["status"] = "Warning";
                TempData["message"] = "User name and/or password are incorrect";
                return RedirectToAction("Index");
            }
            else
            {
                return RedirectToAction("Main");
            }
        }

        [SystemAuthorize(Roles = "Admin,Tester,Developer,Project Manager")]
        public ActionResult SignOut()
        {
            Session.Remove("Username");
            Session.Remove("Status");
            Session.Remove("Id");
            Session.Remove("Confirmed");

            HttpCookie nick = new HttpCookie("username");
            nick.Expires = DateTime.Now.AddDays(-1d);
            Response.SetCookie(nick);

            HttpCookie pass = new HttpCookie("password");
            pass.Expires = DateTime.Now.AddDays(-1d);
            Response.SetCookie(pass);

            return RedirectToAction("Index");
        }

        public ActionResult SignUp()
        {
            User user = new User();
            return View(user);
        }

        [HttpPost]
        public ActionResult SignUp(User u, HttpPostedFileBase userAvatar, string confirmPassword, string birthDate)
        {

            if (birthDate != "")
            {
                u.BirthDate = DateTime.ParseExact(birthDate, "dd.MM.yyyy",
                                       System.Globalization.CultureInfo.InvariantCulture);
            }

            if (ModelState.IsValid)
            {
                if (u.Password == confirmPassword)
                {
                    if (userAvatar != null)
                    {
                        u.Avatar = new byte[userAvatar.ContentLength];
                        userAvatar.InputStream.Read(u.Avatar, 0, userAvatar.ContentLength);
                    }

                        string addResult = service.AddAccount(u);

                        if (addResult == "Success")
                        {

                            int insertedId = service.getUsers().Where(x => x.Nickname == u.Nickname).ToList().First().Id;

                            string text = "Please confirm the personality of new user <a href=\"" + Url.Action("ExternalAccountPage", "Bts", new { @id = insertedId }) + "\">" + u.Nickname + "</a>";

                            List<User> admins = service.getUsers().Where(x => x.Status == "Admin").ToList();

                            foreach (var admin in admins)
                            {
                                service.WriteMessage(admin.Nickname, u.Nickname, text);
                                service.InformAboutNotification(admin);
                            }
                        }
                        else if (addResult == "Fail")
                        {
                            TempData["status"] = "Error";
                            TempData["message"] = "Internal error. Try again. The problem may be with your birth date";
                        }
                        else
                        {
                            TempData["status"] = "Warning";
                            TempData["message"] = "User with the same nickname or e-mail has already been registered";
                        }

                        if (addResult == "Success")
                        {
                            return RedirectToAction("Index", "Bts",
                                new { messageType = "Success", message = "Account has been successfully registered" });
                        }
                    else
                    {
                        TempData["status"] = "Error";
                        TempData["message"] = "Internal error. Try again";
                    }
                }
                else
                {
                    TempData["status"] = "Warning";
                    TempData["message"] = "Passwords don't match";
                }
            }

            return View();
        }

        public ActionResult PasswordUpdate()
        {
            return View();
        }

        [HttpPost]
        public ActionResult PasswordUpdate(string email)
        {
            if (email == "")
            {
                TempData["status"] = "Notification";
                TempData["message"] = "Please fill in the input form";
            }
            else
            {
                    bool letterSent = false;

                    if (email != "")
                    {
                        letterSent = service.isEmailSent(email);
                    }

                    if(letterSent)
                    {
                        return RedirectToAction("Index", "Bts",
                            new { messageType = "Success", message = "Letter was successfully sent" });
                    }
                    else
                    {
                        TempData["message"] = "Error";
                        TempData["message"] = "Letter was not sent. Try again";
                    }
            }
            return View();
        }

        [SystemAuthorize(Roles = "Admin,Tester,Developer,Project Manager")]
        public ActionResult Main(string message, string messageType)
        {
            List<Category> categories = new List<Category>();

            categories = service.getCategories();

            if(messageType != null && message != null)
            {
                TempData["message"] = message;
                TempData["status"] = messageType;
            }

            return View(categories);
        }

        [HttpPost]
        public string GetProjectsByName(string[] prName)
        {
            List<Project> toReturn = null;

                toReturn = service.GetProjectsByName(prName[0]);


            List<string> imageUrls = new List<string>();

            if (toReturn != null && toReturn.Count() != 0)
            {
                foreach (Project proj in toReturn)
                {
                    var base64 = Convert.ToBase64String(proj.Logo);
                    string toAdd = string.Format("data:image/jpg;base64, {0}", base64);
                    imageUrls.Add(toAdd);
                    proj.Logo = null;
                }
            }

            string returnStringProjects = JsonConvert.SerializeObject(toReturn);

            if (toReturn != null && toReturn.Count() != 0)
            {
                string returnStringImgs = JsonConvert.SerializeObject(imageUrls);

                returnStringProjects = returnStringProjects.Substring(0, returnStringProjects.Count() - 1);
                returnStringProjects += ",";
                returnStringImgs = returnStringImgs.Substring(1);

                returnStringProjects += returnStringImgs;
            }

            return returnStringProjects;
        }

        [HttpPost]
        public string GetProjectsByCategories(int[] categories)
        {
            List<Project> toReturn = null;

            toReturn = service.GetProjectsByCategories(categories);


            List<string> imageUrls = new List<string>();

            if (toReturn != null && toReturn.Count() != 0)
            {
                foreach (Project proj in toReturn)
                {
                    var base64 = Convert.ToBase64String(proj.Logo);
                    string toAdd = string.Format("data:image/jpg;base64, {0}", base64);
                    imageUrls.Add(toAdd);
                    proj.Logo = null;
                }
            }

            string returnStringProjects = JsonConvert.SerializeObject(toReturn);

            if (toReturn != null && toReturn.Count() != 0)
            {
                string returnStringImgs = JsonConvert.SerializeObject(imageUrls);

                returnStringProjects = returnStringProjects.Substring(0, returnStringProjects.Count() - 1);
                returnStringProjects += ",";
                returnStringImgs = returnStringImgs.Substring(1);

                returnStringProjects += returnStringImgs;
            }

            return returnStringProjects;
        }

        [SystemAuthorize(Roles="Project Manager,Admin")]
        public ActionResult AddProject()
        {
            Project proj = new Project();

                List<Category> categories = service.getCategories();


                if (categories != null)
                { 

                    List<SelectListItem> categoryItems = new List<SelectListItem>();

                    foreach(Category category in categories)
                    {
                        SelectListItem item = new SelectListItem()
                        {
                            Text = category.Title,
                            Value = category.Title
                        };

                        categoryItems.Add(item);
                    }

                    ViewBag.Categories = categoryItems;

                    return View(proj);
                }

            return View("PageNotFound", "Error");
        }

        [HttpPost]
        public ActionResult AddProject(Project proj, HttpPostedFileBase aLogo, FormCollection collection)
        {

            List<Category> categories = null;

            if (ModelState.IsValid)
            {
                proj.PmId = int.Parse(Session["Id"].ToString());

                if (collection["projectCategories"] != null)
                {
                    List<string> items = collection["projectCategories"].ToString().Split(',').ToList();

                    categories = service.getCategories();
                        
                    if (categories != null)
                    {

                        List<int> categoryIds = new List<int>();

                        foreach (string categoryName in items)
                        {
                            int categoryId = categories.Where(x => x.Title == categoryName).Single().Id;

                            categoryIds.Add(categoryId);
                        }

                        if (aLogo != null)
                        {
                            byte[] img = new byte[aLogo.ContentLength];
                            aLogo.InputStream.Read(img, 0, aLogo.ContentLength);

                            proj.Logo = img;
                        }

                        bool success = false;

                            success = service.AddProject(proj, categoryIds);


                        if (success)
                        {
                            return RedirectToAction("InternalAccountPage", "Bts", 
                                new { messageType = "Success", message = "New project has been successfully added" });
                        }
                    }
                }
            }

                categories = service.getCategories();

                if (categories != null)
                {
                    List<SelectListItem> categoryItems = new List<SelectListItem>();

                    foreach (Category category in categories)
                    {
                        SelectListItem item = new SelectListItem()
                        {
                            Text = category.Title,
                            Value = category.Title
                        };

                        categoryItems.Add(item);
                    }

                    ViewBag.Categories = categoryItems;
                    return View();
                }

                return View("PageNotFound", "Error");
        }

        [SystemAuthorize(Roles = "Admin,Tester,Developer,Project Manager")]
        public ActionResult ShowProject(string name, string message, string messageType)
        {
            bool projectFound = false;

            Project project = new Project();

            List<Bug> projectBugs = null;

                List<Project> chosenProjects = service.GetProjectsByName(name);

                if (chosenProjects.Count > 0)
                {
                    projectFound = true;
                    project = chosenProjects[0];
                }

                if (project != null)
                {
                    List<User> invitedDevs = null;
                    List<User> developers = service.GetDevelopersOfProject(name, out invitedDevs);
                    List<SelectListItem> developerList = null;

                    ViewBag.InvitedDevs = invitedDevs;
                    ViewBag.ApprovedDevelopers = developers;
                    

                    if (developers != null)
                    {
                        developerList = new List<SelectListItem>();

                        foreach (User dev in developers)
                        {
                            SelectListItem item = new SelectListItem() { Text = dev.Name + " " + dev.Surname, Value = dev.Id.ToString() };
                            developerList.Add(item);
                        }
                    }

                    ViewBag.Developers = developerList;
                }

                projectBugs = service.GetProjectBugs(project);


            if (!projectFound)
            {
                return RedirectToAction("PageNotFound", "Error");
            }

            if (projectBugs != null)
            {
                ViewBag.ProjectBugs = projectBugs;
            }
            else
            {
                ViewBag.ProjectBugs = new List<Bug>();
            }

            ViewBag.Pm = project.PmId;

            if(messageType != null && message != null)
            {
                TempData["message"] = message;
                TempData["status"] = messageType;
            }

            return View(project);
        }

        public ActionResult JoinProject(int id, string projectName)
        {
            service.ApproveDeveloperForProject(projectName, id);

            return RedirectToAction("ShowProject", "Bts", new { name = projectName });
        }

        public ActionResult ChangePassword()
        {
                if (!service.IsPasswordResetLinkValid(Request.QueryString["uid"]))
                {
                    TempData["status"] = "Error";
                    TempData["message"] = "Password Reset link has expired or is invalid";
                }

            return View();
        }

        [HttpPost]
        public string FindDevelopersForProject(int id, string[] names, string projectName)
        {
            List<User> toReturn = new List<User>();
            List<string> imgPaths = null;
            User anonymous = null;
            List<string> userNames = names.Distinct().ToList();
            userNames.Remove("");

                anonymous = service.getUsers().Where(x => x.Nickname == "Anonymous").ToList()[0];

                List<User> invitedDevs = null;
                List<User> currDevelopers = service.GetDevelopersOfProject(projectName, out invitedDevs);

                if(invitedDevs.Count() > 0)
                {
                    if(currDevelopers == null)
                    {
                        currDevelopers = new List<User>();
                    }

                    currDevelopers.AddRange(invitedDevs);
                }

                while (toReturn.Count() < 5)
                {
                    int prevCount = toReturn.Count();
                    List<User> buf = null;

                    buf = service.searchForUsers(id, userNames, new List<string> { "Developer" });

                    if(buf != null)
                    {
                        id = buf.Last().Id;

                        buf = buf.Where(x => x.Confirmed).ToList();
                    }

                    if(buf == null)
                    {
                        break;
                    }
                    else
                    {
                        foreach(User newDeveloper in buf)
                        {
                            bool elFound = false;

                            if (currDevelopers != null)
                            {
                                foreach (User existingDeveloper in currDevelopers)
                                {
                                    if (existingDeveloper.Id == newDeveloper.Id)
                                    {
                                        elFound = true;
                                        break;
                                    }
                                }
                            }

                            if(!elFound)
                            {
                                toReturn.Add(newDeveloper);

                                if(toReturn.Count() == 5)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }

            if (toReturn != null)
            {
                int size = toReturn.Count();

                imgPaths = new List<string>();

                for (int i = 0; i < size; ++i)
                {
                    if (toReturn[i].Avatar != null)
                    {
                        var base64 = Convert.ToBase64String(toReturn[i].Avatar);
                        string toAdd = string.Format("data:image/jpg;base64, {0}", base64);
                        imgPaths.Add(toAdd);
                        toReturn[i].Avatar = null;
                    }
                    else
                    {
                        User u = new User();
                        var base64 = Convert.ToBase64String(anonymous.Avatar);
                        string toAdd = string.Format("data:image/jpg;base64, {0}", base64);
                        imgPaths.Add(toAdd);
                    }
                }
            }

            if(toReturn.Count() == 0)
            {
                toReturn = null;
            }

            string toReturnString = JsonConvert.SerializeObject(toReturn);

            if (toReturn != null)
            {
                toReturnString = toReturnString.Substring(0, toReturnString.Count() - 1);
                toReturnString += ",";

                string toReturnStringImgs = JsonConvert.SerializeObject(imgPaths);
                toReturnStringImgs = toReturnStringImgs.Substring(1);

                toReturnString += toReturnStringImgs;
            }

            return toReturnString;
        }

        [SystemAuthorize(Roles = "Admin,Tester,Developer,Project Manager")]
        public ActionResult InviteDevelopers(string projectName)
        {
            if (projectName != null)
            {
                ViewBag.ProjectName = projectName;

                return View();
            }

            return RedirectToAction("PageNotFound", "Error");
        }

        [HttpPost]
        public string GetUserNotifications(string receiver)
        {
            List<Notification> toReturn = null;

                toReturn = service.GetNotificationsOfUser(receiver);

            return JsonConvert.SerializeObject(toReturn);
        }

        [HttpPost]
        public string InviteDevelopers(int devId, string projectName)
        {
            bool toReturn = false;

                toReturn = service.InviteDeveloperToProject(projectName, devId);

                if(toReturn)
                {
                    Project proj = service.GetProjectsByName(projectName).ToList().SingleOrDefault();

                    User dev = service.getUsers().Where(x => x.Id == devId).ToList().SingleOrDefault();

                    User admin = service.getUsers().Where(x => x.Status == "Admin").ToList()[0];

                    string text = "You were invited to the <a href=\"" + Url.Action("ShowProject", "Bts", new { name = proj.Name }) + "\">" + projectName + "</a> project. Please go to the project page to accept invitation";

                    service.WriteMessage(dev.Nickname, admin.Nickname, text);
                    service.InformAboutNotification(dev);
                }

   
            return JsonConvert.SerializeObject(toReturn);
        }

        [HttpPost]
        public ActionResult ChangePassword(string password, string confirmPassword)
        {
            if (password == confirmPassword)
            {
                    bool passwordChanged = service.ChangeUserPassword(Request.QueryString["uid"], password);

                    if(!passwordChanged)
                    {
                        TempData["status"] = "Error";
                        TempData["message"] = "Password Reset link has expired or is invalid";
                    }

                    service.deleteExpiredRecords();

                    if(passwordChanged)
                    {
                        return RedirectToAction("Index", "Bts", 
                            new { messageType = "Success", message = "Password successfully changed" });
                    }
            }
            else
            {
                TempData["status"] = "Warning";
                TempData["message"] = "Passwords don't match";
            }

            return View();
        }

        [SystemAuthorize(Roles = "Admin,Tester,Developer,Project Manager")]
        public ActionResult ReportBug(int projectId, string projectName)
        {
            TempData["projId"] = projectId;
            TempData["projname"] = projectName;
            Bug bug = new Bug();
            return View(bug);
        }

        [SystemAuthorize(Roles = "Admin,Tester,Developer,Project Manager")]
        public ActionResult FindUsers()
        {
            List<string> statuses = service.getUsers().Select(x => x.Status).ToList().Distinct().ToList();

            ViewBag.StatusList = statuses;
            return View();
        }

        [HttpPost]
        public string RemoveDevelopersFromProject(string projName, int[] toErase)
        {
            bool toReturn = false;

            if (toErase != null)
            {
                    toReturn = service.RemoveDevsFromProject(projName, toErase.ToList());
            }

           if(toReturn && toErase != null)
            {
                    Project proj = service.GetProjectsByName(projName).Single();
                    User pm = service.getUsers().Where(x => x.Id == proj.PmId).Single();

                    foreach (int devId in toErase)
                    {
                        User user = service.getUsers().Where(x => x.Id == devId).Single();


                        string text1 = "Project manager dismissed you from project <a href=\"" + Url.Action("ShowProject", "Bts", new { name = projName }) + "\">"
                            + projName + "</a> . Do not despair and move on!";
                        service.WriteMessage(user.Nickname, pm.Nickname, text1);
                    }

            }

            return JsonConvert.SerializeObject(toReturn);
        }

        [HttpPost]
        public string FindUsers(int id, string[]  names, string[] statusItems)
        {
            List<User> toReturn = null;
            List<string> userStatuses = null;
            List<string> userNames = names.Distinct().ToList();
            List<string> imgPaths = null;
            User anonymous = null;
            userNames.Remove("");

            if (statusItems != null)
            {
                userStatuses = statusItems.ToList();
            }

                toReturn = service.searchForUsers(id, userNames, userStatuses);
                anonymous = service.getUsers().Where(x => x.Nickname == "Anonymous").ToList()[0];


            if (toReturn != null)
            {
                int size = toReturn.Count();

                imgPaths = new List<string>();

                for (int i = 0; i < size; ++i)
                {
                    if (toReturn[i].Avatar != null)
                    {
                        var base64 = Convert.ToBase64String(toReturn[i].Avatar);
                        string toAdd = string.Format("data:image/jpg;base64, {0}", base64);
                        imgPaths.Add(toAdd);
                        toReturn[i].Avatar = null;
                    }
                    else
                    {
                        User u = new User();
                        var base64 = Convert.ToBase64String(anonymous.Avatar);
                        string toAdd = string.Format("data:image/jpg;base64, {0}", base64);
                        imgPaths.Add(toAdd);
                    }
                }
            }

            string toReturnString = JsonConvert.SerializeObject(toReturn);

            if (toReturn != null)
            {
                toReturnString = toReturnString.Substring(0, toReturnString.Count() - 1);
                toReturnString += ",";

                string toReturnStringImgs = JsonConvert.SerializeObject(imgPaths);
                toReturnStringImgs = toReturnStringImgs.Substring(1);

                toReturnString += toReturnStringImgs;
            }

            return toReturnString;
        }

        [HttpPost]
        public string RemoveNotification(int id)
        {
            bool toReturn = false;

            toReturn = service.RemoveNotification(id);


            return JsonConvert.SerializeObject(toReturn);
        }

        [SystemAuthorize(Roles = "Admin,Tester,Developer,Project Manager")]
        public ActionResult InternalAccountPage(string message, string messageType)
        {
            User user = new User();


                user = service.getUsers().Where(x => x.Nickname == Session["Username"].ToString()).Single();
                List<User> users = service.getUsers();

                if (user.Avatar == null)
                {
                    User anonymous = users.Single(x => x.Name == "Anonymous");
                    var base64 = Convert.ToBase64String(anonymous.Avatar);
                    ViewBag.ImgPath = string.Format("data:image/jpg;base64, {0}", base64);
                }
                else
                {
                    var base64 = Convert.ToBase64String(user.Avatar);
                    ViewBag.ImgPath = string.Format("data:image/jpg;base64, {0}", base64);
                }
            
            
            if(user == null)
            {
                return RedirectToAction("PageNotFound", "Error");
            }

            if(messageType != null && message != null)
            {
                TempData["message"] = message;
                TempData["status"] = messageType;
            }

            return View(user);
        }

        [SystemAuthorize(Roles = "Admin,Tester,Developer,Project Manager")]
        public ActionResult ExternalAccountPage(int id)
        {
            User u = null;
            List<User> users = null;

                users = service.getUsers();
                u = users.SingleOrDefault(x => x.Id == id);


            if(u == null)
            {
                return RedirectToAction("PageNotFound", "Error");
            }
            else if(u.Nickname == Session["Username"].ToString())
            {
                return RedirectToAction("InternalAccountPage");
            }

            if (u.Avatar == null)
            {
                User anonymous = users.Single(x => x.Name == "Anonymous");
                var base64 = Convert.ToBase64String(anonymous.Avatar);
                ViewBag.ImgPath = string.Format("data:image/jpg;base64, {0}", base64);
            }
            else
            {
                var base64 = Convert.ToBase64String(u.Avatar);
                ViewBag.ImgPath = string.Format("data:image/jpg;base64, {0}", base64);
            }

            return View(u);
        }

        [HttpPost]
        public string EditEmail(int id,string email)
        {
            bool toReturn = false;

                toReturn = service.EditUserEmail(id, email);

            return JsonConvert.SerializeObject(toReturn);
        }

        [HttpPost]
        public string EditBirthDate(int id, string birthdate)
        {
            bool toReturn = false;

                toReturn = service.EditUserBirthDate(id, birthdate);


            return JsonConvert.SerializeObject(toReturn);
        }

        [HttpPost]
        public string EditAvatar(int id, HttpPostedFileBase avatar)
        {
            User u = new User();
            bool success = false;
            byte[] toPost = new byte[avatar.ContentLength];

                avatar.InputStream.Read(toPost, 0, avatar.ContentLength);

                success = service.EditUserAvatar(id, toPost);


            string toReturn = null;

            if (success)
            {
                var base64 = Convert.ToBase64String(toPost);
                toReturn = string.Format("data:image/jpg;base64, {0}", base64);
            }
            

            return toReturn;
        }

    [ValidateInput(false)]
        [HttpPost]
        public ActionResult ReportBug(Bug b)
        {

            bool bugReported = false;
            bool attachmentsAdded = true;
            
            if (TempData["projId"] != null)
            { 
            b.ProjectId = Convert.ToInt32(TempData["projId"].ToString());
            b.Status = "Pending";
            b.TopicStarter = Session["Username"].ToString();

                if (ModelState.IsValid)
                {
                    string encodedDescripton = HttpUtility.HtmlEncode(b.Description);
                    string encodedSubject = HttpUtility.HtmlEncode(b.Subject);

                    b.Subject = encodedSubject;
                    b.Description = encodedDescripton;

                    Uri tcpUri = new Uri("http://localhost:1050/TestService");
                    EndpointAddress address = new EndpointAddress(tcpUri);
                    BasicHttpBinding binding = new BasicHttpBinding();
                    ChannelFactory<IService> factory = new ChannelFactory<IService>(binding, address);
                    IService service = factory.CreateChannel();

                    bugReported = service.ReportBug(b);
                }

                TempData["projId"] = b.ProjectId;
            }

            if (bugReported && attachmentsAdded)
            {
                
                return RedirectToAction("ShowProject", "Bts",
                    new { name=TempData["projName"].ToString(), messageType = "Success", message = "Bug successfully reported. Wait for further messages" });
            }
            else
            {
                TempData["status"] = "Error";
                TempData["message"] = "Something went wrong. Probably the bug has already been noticed or files are too heavy";
            }

            return View();
        }

        [SystemAuthorize(Roles = "Admin,Tester,Developer,Project Manager")]
        public ActionResult BugDescriptionPage(string projName, int id)
        {
            ViewBag.ProjectName = projName;

                Project proj = service.GetProjectsByName(projName)[0];

                Bug bug = null;

                if (proj != null)
                {
                    bug = service.GetProjectBugs(proj).Where(x => x.Id == id).ToList()[0];
                }

                if (bug != null)
                {
                    List<User> invitedDevs = null;
                    List<User> developers = service.GetDevelopersOfProject(projName, out invitedDevs);
                    List<SelectListItem> developerList = null;

                    if (developers != null)
                    {
                        developerList = new List<SelectListItem>();

                        foreach (User dev in developers)
                        {
                            SelectListItem item = new SelectListItem() { Text = dev.Nickname, Value = dev.Nickname };
                            developerList.Add(item);
                        }
                    }

                    ViewBag.Developers = developerList;

                    if (bug.DeveloperId != 0)
                    {
                        ViewBag.DeveloperInDuty = service.getUsers().Where(x => x.Id == bug.DeveloperId).ToList().SingleOrDefault();
                    }
                    else
                    {
                        ViewBag.DeveloperInDuty = null;
                    }

                    if (proj.PmId != 0)
                    {
                        ViewBag.ProjectManager = service.getUsers().Where(x => x.Id == proj.PmId).ToList().SingleOrDefault();
                    }
                    else
                    {
                        ViewBag.ProjectManager = null;
                    }

                     

                    return View(bug);
                }

            return RedirectToAction("PageNotFound", "Error");
        }

        [HttpPost]
        public string ChangeDeveloper(int bugId, string projectName, string devNickname)
        {

            bool toReturn = false;

                User u = service.getUsers().Where(x => x.Nickname == devNickname).ToList().SingleOrDefault();

                Project proj = service.GetProjectsByName(projectName).SingleOrDefault();

                Bug toChange = service.GetProjectBugs(proj).Where(x => x.Id == bugId).ToList().SingleOrDefault();

                User prevDeveloper = service.getUsers().Where(x => x.Id == toChange.DeveloperId).ToList().SingleOrDefault();
                User pm = service.getUsers().Where(x => x.Id == proj.PmId).ToList().SingleOrDefault();
                User newDeveloper = service.getUsers().Where(x => x.Nickname == devNickname).Single();

                if (prevDeveloper != null)
                {
                    if (prevDeveloper.Name != devNickname)
                    {
                        string text1 = "Project manager made <a href=\"" + Url.Action("ExternalAccountPage", "Bts", new { id = newDeveloper.Id })
                            + "\">" + devNickname + "</a> responsible for <a href=\"" + Url.Action("BugDescriptionPage", "Bts", new { id = bugId, projName = projectName }) + "\">" + "bug #" + bugId
                            + "</a> in project <a href=\"" + Url.Action("ShowProject", "Bts", new { name = projectName }) + "\">" + projectName + "</a> instead of you. Do not despair and move on!";
                        service.WriteMessage(prevDeveloper.Nickname, pm.Nickname, text1);
                        service.InformAboutNotification(prevDeveloper);

                        string text2 = "You were made responsible for <a href=\"" + 
                            Url.Action("BugDescriptionPage", "Bts", new { id = bugId, projName = projectName }) + "\">" + "bug #" + bugId +
                            "</a> in project <a href=\"" + Url.Action("ShowProject", "Bts", new { name = projectName }) + "\">" + projectName + 
                            "</a>. Have a luck!";

                        service.WriteMessage(devNickname, pm.Nickname, text2);
                        service.InformAboutNotification(newDeveloper);
                }
                else
                {

                    string text2 = "You were made responsible for <a href=\"" +
                        Url.Action("BugDescriptionPage", "Bts", new { id = bugId, projName = projectName }) + "\">" + "bug #" + bugId +
                        "</a> in project <a href=\"" + Url.Action("ShowProject", "Bts", new { name = projectName }) + "\">" + projectName +
                        "</a>. Have a luck!";

                    service.WriteMessage(devNickname, pm.Nickname, text2);
                }      
            }


            if (u != null)
            {
                toReturn = service.SetDevIdForBug(bugId, u.Id);
                service.SetBugStatus(bugId, "Assigned");
            }

            return JsonConvert.SerializeObject(toReturn);
        }

        [HttpPost]
        public ActionResult RestartBug(string projName, int id)
        {

                service.RestartBug(id);

                Project proj = service.GetProjectsByName(projName).ToList().SingleOrDefault();
                Bug bug = service.GetProjectBugs(proj).Where(x => x.Id == id).ToList().SingleOrDefault();

                User bugDeveloper = service.getUsers().Where(x => x.Id == bug.DeveloperId).ToList().SingleOrDefault();
                User pm = service.getUsers().Where(x => x.Id == proj.PmId).ToList().SingleOrDefault();
                User topicStarter = service.getUsers().Where(x => x.Nickname == bug.TopicStarter).Single();

                string message = "<a href=\"" + Url.Action("ExternalAccountPage", "Bts", new { id = topicStarter.Id })
                + "\">" + bug.TopicStarter + "</a> restarted discusion of <a href=\"" + Url.Action("BugDescriptionPage", "Bts", new { id = id, projName = projName }) + "\">" + "bug #" + id
                + "</a> in project <a href=\"" + Url.Action("ShowProject", "Bts", new { name = projName }) + "\">" + projName + "</a>";

                service.WriteMessage(bugDeveloper.Nickname, bug.TopicStarter, message);
                service.InformAboutNotification(bugDeveloper);

                service.WriteMessage(pm.Nickname, bug.TopicStarter, message);
                service.InformAboutNotification(pm);

                 

                return RedirectToAction("BugDescriptionPage", "Bts", new { projName = projName, id = id });

        }

        [SystemAuthorize(Roles = "Admin,Tester,Developer,Project Manager")]
        public ActionResult BugWorkflowPage(string projName, int id)
        {
            ViewBag.ProjectName = projName;

                Project proj = service.GetProjectsByName(projName)[0];

                Bug bug = null;

                if (proj != null)
                {
                    bug = service.GetProjectBugs(proj).Where(x => x.Id == id).ToList()[0];
                }

                if (bug != null)
                {
                    List<Message> messages = service.GetMessageLog(id);

                    int correct = -1;

                    if (messages != null)
                    {
                        messages = messages.OrderByDescending(x => x.Id).ToList();

                        foreach (Message message in messages)
                        {
                            if (message.Correct)
                            {
                                correct = message.Id;
                            }
                        }
                    }

                    if(correct != -1)
                    {
                        ViewBag.CorrectItem = correct;
                    }
                    else
                    {
                        ViewBag.CorectItem = 0;
                    }

                    ViewBag.Messages = messages;

                    return View(bug);
            }

            return RedirectToAction("PageNotFound", "Error");
        }

        [HttpPost]
        public ActionResult BugWorkflowPage(int id, string messageToAdd, string projName, int recipientId, string Recipient)
        {
            ViewBag.ProjectName = projName;

            string[] urlSearch = messageToAdd.Split(' ');

            for(int i = 0; i < urlSearch.Count(); ++i)
            {
                if(urlSearch[i].Length > 10 && urlSearch[i].Substring(0, 7) == "http://")
                {
                    urlSearch[i] = "<a href=\"" + urlSearch[i] + "\">" + urlSearch[i] + "</a>";
                }
            }

            StringBuilder builder = new StringBuilder();
            builder.Append("<b>");

            for(int i = 0; i < urlSearch.Count(); ++i)
            {
                builder.Append(urlSearch[i]);
                builder.Append(" ");
            }

            builder.Append("</b>");

            messageToAdd = builder.ToString();

            messageToAdd.Replace(' ', '_');

            messageToAdd.Replace("\r\n", "<br/>");

                Bug bug = null;

                Message message = new Message();
                message.SenderNick = Session["Username"].ToString();
                message.UserToReply = Recipient;
                message.MessageToReplyId = recipientId;
                message.MessageText = messageToAdd;

                service.AddMessageToWorkflow(id, message);

                Project proj = service.GetProjectsByName(projName).ToList().SingleOrDefault();


                if (proj != null)
                {
                    bug = service.GetProjectBugs(proj).Where(x => x.Id == id).ToList().SingleOrDefault();
                }

                if (bug != null)
                {
                    User developer = service.getUsers().Where(x => x.Id == bug.DeveloperId).ToList().SingleOrDefault();

                    if (developer != null)
                    {
                        if (Session["Username"].ToString() == bug.TopicStarter)
                        {
                            string messageText = "<a href =\"" + Url.Action("ExternalAccountPage", "Bts", new { id = developer.Id })
                                  + "\">" + bug.TopicStarter + "</a>  added new message to discussion of <a href=\""
                                  + Url.Action("BugDescriptionPage", "Bts", new { id = bug.Id, projName = projName }) + "\"> bug # " + id + "</a>";

                            service.WriteMessage(developer.Nickname, bug.TopicStarter, messageText);
                            service.InformAboutNotification(developer);
                        }
                        else
                        {
                            string messageText = "<a href =\"" + Url.Action("ExternalAccountPage", "Bts", new { id = developer.Id })
                            + "\">" + developer.Nickname + "</a>  added new message to discussion of <a href=\""
                            + Url.Action("BugDescriptionPage", "Bts", new { id = bug.Id, projName = projName }) + "\"> bug # " + id + "</a>";

                            User topicStarter = service.getUsers().Where(x => x.Nickname == bug.TopicStarter).Single();

                            service.WriteMessage(bug.TopicStarter, developer.Nickname, messageText);
                            service.InformAboutNotification(topicStarter);
                        }
                    }

                    List<Message> messages = service.GetMessageLog(id);

                    if (messages != null)
                    {
                        messages = messages.OrderByDescending(x => x.Id).ToList();
                    }

                    ViewBag.Messages = messages;
                }

                 

                return RedirectToAction("BugDescriptionPage", "Bts", new { id = id, projName = projName });
        }

        [HttpPost]
        public string MarkRightAnswer(int selectedItemId, int bugId, int estimate, string finalComment, string projectName, string userToReply)
        {
            bool toReturn = false;

                Message aMessage = new Message();
                aMessage.MessageText = finalComment;
                aMessage.SenderNick = Session["Username"].ToString();
                aMessage.MessageToReplyId = selectedItemId;
                aMessage.UserToReply = userToReply;

                service.AddMessageToWorkflow(bugId, aMessage);

                toReturn = service.MarkRightIssueAnswer(bugId, selectedItemId, estimate);

                if(toReturn)
                {
                    Project proj = service.GetProjectsByName(projectName).ToList().SingleOrDefault();
                    Bug bug = service.GetProjectBugs(proj).Where(x => x.Id == bugId).ToList().SingleOrDefault();

                    User bugDeveloper = service.getUsers().Where(x => x.Id == bug.DeveloperId).ToList().SingleOrDefault();
                    User pm = service.getUsers().Where(x => x.Id == proj.PmId).ToList().SingleOrDefault();
                    User topicStarter = service.getUsers().Where(x => x.Nickname == bug.TopicStarter).Single();

                    string message ="<a href=\"" + Url.Action("ExternalAccountPage", "Bts", new { id = topicStarter.Id })
                    + "\">" + bug.TopicStarter + "</a> closed discusion of <a href=\"" + Url.Action("BugDescriptionPage", "Bts", new { id = bugId, projName = projectName }) + "\">" + "bug #" + bugId
                    + "</a> in project <a href=\"" + Url.Action("ShowProject", "Bts", new { name = projectName }) + "\">" + projectName + "</a>";

                    service.WriteMessage(bugDeveloper.Nickname, bug.TopicStarter, message);
                    service.InformAboutNotification(bugDeveloper);

                    service.WriteMessage(pm.Nickname, bug.TopicStarter, message);
                    service.InformAboutNotification(pm);
                }

            return JsonConvert.SerializeObject(toReturn);
        }

        [SystemAuthorize(Roles= "Admin,Tester,Developer,Project Manager")]
        public ActionResult BugSolutionPage(int id, string projName)
        {

            ViewBag.ProjectName = projName;

                Project proj = service.GetProjectsByName(projName)[0];

                Bug bug = null;

                if (proj != null)
                {
                    bug = service.GetProjectBugs(proj).Where(x => x.Id == id).ToList()[0];
                }

                if (bug != null)
                {
                    ViewBag.PmId = proj.PmId;

                    return View(bug);
                }

            return RedirectToAction("PageNotFound", "Error");
        }

        [HttpPost]
        [ValidateInput(false)]
        public string BugSolution(int bugId, string solution, string projectName)
        {
            bool toReturn = true;

                toReturn = service.AddSolutionOfBug(bugId, solution);

                if(toReturn)
                {
                    Project proj = service.GetProjectsByName(projectName).ToList().FirstOrDefault();

                    Bug bug = service.GetProjectBugs(proj).Where(x => x.Id == bugId).ToList().FirstOrDefault();

                    User pm = service.getUsers().Where(x => x.Id == proj.PmId).ToList().SingleOrDefault();

                    User topicStarter = service.getUsers().Where(x => x.Nickname == bug.TopicStarter).Single();

                    string message = "<a href=\"" + Url.Action("ExternalAccountPage", "Bts", new { id = pm.Id })
                    + "\">" + pm.Nickname + "</a> documented solution of <a href=\"" + Url.Action("BugDescriptionPage", "Bts", new { id = bugId, projName = projectName }) + "\">" + "bug #" + bugId
                    + "</a> in project <a href=\"" + Url.Action("ShowProject", "Bts", new { name = projectName }) + "\">" + projectName + "</a>";

                    service.WriteMessage(bug.TopicStarter, pm.Nickname, message);
                    service.InformAboutNotification(topicStarter);
                }


            return JsonConvert.SerializeObject(toReturn);
        }
    }
}