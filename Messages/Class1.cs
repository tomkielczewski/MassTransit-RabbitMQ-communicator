using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messages
{
    public interface IPubl
    {
        int Number { get; set; }
    }
    public interface ISwitch
    {
        bool IsOn { get; set; }
    }
    public interface IAnswer   
    {
        string Who { get; set; }
    }
    public interface IAnswerA : IAnswer
    {
    }
    public interface IAnswerB : IAnswer 
    {
    }
}
