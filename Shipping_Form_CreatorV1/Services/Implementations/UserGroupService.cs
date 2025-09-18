using System.DirectoryServices.AccountManagement;

namespace Shipping_Form_CreatorV1.Services.Implementations
{
    public class UserGroupService
    {
        public static bool IsCurrentUserInDittoGroup()
        {
            const string groupName = "Ditto Sales";
            try
            {
                using var context = new PrincipalContext(ContextType.Domain);
                var user = UserPrincipal.Current;
                var groups = user.GetAuthorizationGroups();

                foreach (var group in groups)
                {
                    if (group.Name != null && group.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            catch (Exception ex)
            {
                // Handle or log exception appropriately
                Console.WriteLine($"Error checking group: {ex.Message}");
            }

            return false;
        }
    }
}
