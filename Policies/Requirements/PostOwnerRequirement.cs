using Microsoft.AspNetCore.Authorization;

namespace BlogApplication.Policies.Requirements{

    public class PostOwnerRequirement: IAuthorizationRequirement{
      
        public PostOwnerRequirement(){}
    }
}