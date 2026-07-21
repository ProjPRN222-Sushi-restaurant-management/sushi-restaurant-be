using BusinessObjects.Enums;

namespace Services.Policies;

public static class LoyaltyPolicy
{
    public const decimal AmountPerPoint = 10000m;

    public static MembershipLevelEnum CalculateMembershipLevel(int loyaltyPoints)
    {
        return loyaltyPoints switch
        {
            >= 1000 => MembershipLevelEnum.DIAMOND,
            >= 500 => MembershipLevelEnum.GOLD,
            >= 100 => MembershipLevelEnum.SILVER,
            _ => MembershipLevelEnum.NONE
        };
    }

    public static decimal GetDiscountPercent(MembershipLevelEnum membershipLevel)
    {
        return membershipLevel switch
        {
            MembershipLevelEnum.DIAMOND => 15m,
            MembershipLevelEnum.GOLD => 10m,
            MembershipLevelEnum.SILVER => 5m,
            _ => 0m
        };
    }

    public static decimal CalculateDiscountAmount(
        decimal subtotalAmount,
        MembershipLevelEnum membershipLevel)
    {
        if (subtotalAmount <= 0)
        {
            return 0m;
        }

        return Math.Round(
            subtotalAmount * GetDiscountPercent(membershipLevel) / 100m,
            0,
            MidpointRounding.AwayFromZero);
    }

    public static decimal CalculatePayableAmount(
        decimal subtotalAmount,
        MembershipLevelEnum membershipLevel)
    {
        return Math.Max(
            0m,
            subtotalAmount - CalculateDiscountAmount(subtotalAmount, membershipLevel));
    }

    public static int CalculateEarnedPoints(decimal paidAmount)
    {
        if (paidAmount <= 0)
        {
            return 0;
        }

        return (int)Math.Floor(paidAmount / AmountPerPoint);
    }
}
