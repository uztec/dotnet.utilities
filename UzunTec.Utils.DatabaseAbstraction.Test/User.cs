﻿using System;

namespace UzunTec.Utils.DatabaseAbstraction.Test
{
    public class User
    {
        public int UserCode { get; set; }
        public string UserName { get; set; }
        public long? UserCodRef { get; set; }
        public string PasswordMd5 { get; set; }
        public DateTime InputDate { get; set; }
    }
}
