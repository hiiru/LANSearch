using System;

namespace LANSearch.Data.User
{
    [Flags]
    public enum UserRegisterState
    {
        Unknown = 0,
        UserEmpty = 1,
        UserTooShort = 2,
        UserTooLong = 4,
        UserAlreadyTaken = 512,
        EmailEmpty = 8,
        EmailTooLong = 16,
        EmailInvalid = 32,
        PassEmpty = 64,
        PassMissmatch = 128,
        Ok = 256,
        EmailAlreadyUsed = 512,
    }
}