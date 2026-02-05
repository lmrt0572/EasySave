using System;
using System.Collections.Generic;
using System.Text;
using EasySave.Models;

namespace EasySave.Services
{
    public interface IStateService
    {
        void UpdateJobState(BackupJobState state);
    }
}
