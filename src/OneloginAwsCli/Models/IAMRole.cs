namespace OneloginAwsCli.Models
{
    public class IAMRole
    {
        public IAMRole(string role, string principal) =>
            (Role, Principal) = (role, principal);

        public string Role { get; }
        public string Principal { get; }
    }
}
