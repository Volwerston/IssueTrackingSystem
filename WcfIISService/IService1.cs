using BTS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace WcfIISService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    [ServiceContract]
    public interface IService1
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
}
