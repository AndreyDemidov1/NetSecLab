namespace NetSecLab.Modules.Attacks.Generators;

internal static class IpAddressGenerator
{
    public static string CreateAttackSourceIp(Random random)
    {
        return CreateMixedSourceIp(random);
    }

    public static string CreateBackgroundSourceIp(Random random)
    {
        int networkType = random.Next(100);

        if (networkType < 75)
        {
            return "192.168.1." + random.Next(2, 240);
        }

        return CreateMixedSourceIp(random);
    }

    private static string CreateMixedSourceIp(Random random)
    {
        int networkType = random.Next(100);

        if (networkType < 35)
        {
            return "192.168.1." + random.Next(2, 240);
        }

        if (networkType < 60)
        {
            return "10." + random.Next(0, 255) + "." + random.Next(0, 255) + "." + random.Next(2, 240);
        }

        if (networkType < 80)
        {
            return "172.16." + random.Next(0, 32) + "." + random.Next(2, 240);
        }

        return "203.0.113." + random.Next(2, 240);
    }
}
