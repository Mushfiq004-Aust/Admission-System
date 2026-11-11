using api.DTOs.Holds;
using api.Models;

namespace api.Mappers
{
    public static class HoldMapper
    {
        // After "Apply": the new hold plus the class name.
        public static HoldInCreated ToHoldCreated(this AdmissionHold model, string className, DateTime serverTime)
        {
            return new HoldInCreated
            {
                HoldId = model.Id,
                ClassId = model.ClassId,
                ClassName = className,
                StudentId = model.StudentId,
                Status = model.Status,
                ExpiresAt = model.ExpiresAt,
                ServerTime = serverTime
            };
        }

        // For reading an application. A HELD row whose time has passed is reported as EXPIRED
        // even if the background worker has not touched it yet.
        public static HoldInView ToHoldView(this AdmissionHold model, string className, DateTime now)
        {
            return new HoldInView
            {
                HoldId = model.Id,
                ClassId = model.ClassId,
                ClassName = className,
                StudentId = model.StudentId,
                Status = model.Status == HoldStatus.Held && model.ExpiresAt <= now ? HoldStatus.Expired : model.Status,
                ExpiresAt = model.ExpiresAt,
                CreatedAt = model.CreatedAt,
                ConfirmedAt = model.ConfirmedAt,
                ServerTime = now
            };
        }

        public static HoldInConfirmed ToHoldConfirmed(this AdmissionHold model)
        {
            return new HoldInConfirmed
            {
                HoldId = model.Id,
                Status = model.Status,
                ConfirmedAt = model.ConfirmedAt
            };
        }
    }
}
