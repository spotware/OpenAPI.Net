using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OpenAPI.Net
{
    public class OpenClient: IOpenClient
    {
        public Task Connect()
        {
            return Task.CompletedTask;
        }
    }
}
