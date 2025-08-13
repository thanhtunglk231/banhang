using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebApp.Tests.Helpers
{
    public interface ISessionWrapper
    {
        string GetString(string key);
        void SetString(string key, string value);
        void Remove(string key);
    }
}
