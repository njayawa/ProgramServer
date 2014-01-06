using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProgramServer;

namespace ProgramServerMessages
{
    public class TvMessage
    {
        public string Message { get; set; }
        public TvProgramChange ProgramChangeInfo { get; set; }
    }

    public class TvProgramChange
    {
        public const string Message = "PROGRAM_CHANGE";
        public string Id { get; set; }
        public string CallSign { get; set; }
        public string Title { get; set; }
        public string SubTitle { get; set; }
        public string Description { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
    }

    public class MessageCreator
    {
        public static TvMessage CreateMessage(Program p)
        {
            var msg = new TvMessage();
            msg.ProgramChangeInfo = new TvProgramChange();
            msg.ProgramChangeInfo.Id = p.Id;
            msg.ProgramChangeInfo.Title = p.Title;
            msg.ProgramChangeInfo.SubTitle = p.SubTitle;
            msg.ProgramChangeInfo.Description = p.Description;
            msg.ProgramChangeInfo.StartTime = p.StartTime.ToString();
            msg.ProgramChangeInfo.EndTime = p.StartTime.ToString();

            //set message type
            msg.Message = TvProgramChange.Message;
            return msg;
        }
    }
}
