using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DAL.Models.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum VietnameseBankCode
    {
        [Description("Vietnam Bank for Agriculture and Rural Development (Agribank)")]
        Agribank = 970499,

        [Description("Vietnam Joint Stock Commercial Bank for Industry and Trade (Vietinbank)")]
        Vietinbank = 970489,

        [Description("DongA Joint Stock Commercial Bank (DongABank)")]
        DongABank = 970406,

        [Description("Saigon Bank for Industry and Trade (Saigonbank)")]
        Saigonbank = 161087,

        [Description("Joint Stock Bank for Investment and Development of Vietnam (BIDV)")]
        BIDV = 970488,

        [Description("Southeast Asia Joint Stock Commercial Bank (SeABank)")]
        SeABank = 970468,

        [Description("Global Petro Sole Member Limited Commercial Bank (GP.Bank)")]
        GPBank = 970408,

        [Description("Petrolimex Group Commercial Joint Stock Bank (PG Bank)")]
        PGBank = 970430,

        [Description("Vietnam Public Joint-stock Commercial Bank (PVcomBank)")]
        PVcomBank = 970412,

        [Description("Kien Long Commercial Joint-Stock Bank (Kienlongbank)")]
        Kienlongbank = 970452,

        [Description("Viet Capital Commercial Joint Stock Bank (Vietcapital Bank)")]
        VietcapitalBank = 970454,

        [Description("Vietnam Thuong Tin Commercial Joint Stock Bank (VietBank)")]
        VietBank = 970433,

        [Description("Ocean Commercial One Member Limited Joint Stock Bank (OceanBank)")]
        OceanBank = 970414,

        [Description("Sai Gon Thuong Tin Commercial Joint Stock Bank (Sacombank)")]
        Sacombank = 970403,

        [Description("An Binh Commercial Joint Stock Bank (ABBank)")]
        ABBank = 970459,

        [Description("Vietnam Russia Joint Venture Bank (VRB)")]
        VRB = 970421,

        [Description("Joint Stock Commercial Bank for Foreign Trade of Vietnam (Vietcombank)")]
        Vietcombank = 686868,

        [Description("Asia Commercial Joint Stock Bank (ACB)")]
        ACB = 970416,

        [Description("Vietnam Export Import Commercial Joint-Stock Bank (Eximbank)")]
        Eximbank = 452999,

        [Description("Tien Phong Commercial Joint Stock Bank (TPBank)")]
        TPBank = 970423,

        [Description("Saigon - Hanoi Commercial Joint Stock Bank (SHB)")]
        SHB = 970443,

        [Description("Ho Chi Minh City Development Joint Stock Commercial Bank (HDBank)")]
        HDBank = 970437,

        [Description("Military Commercial Joint Stock Bank (MBBank)")]
        MBBank = 970422,

        [Description("Vietnam Prosperity Joint Stock Commercial Bank (VPBank)")]
        VPBank = 981957,

        [Description("Vietnam International Commercial Joint Stock Bank (VIB)")]
        VIB = 180906,

        [Description("VietNam Asia Commercial Joint Stock Bank (VietABank)")]
        VietABank = 166888,

        [Description("Vietnam Technological and Commercial Joint Stock Bank (Techcombank)")]
        Techcombank = 888899,

        [Description("Orient Commercial Joint Stock Bank (OCB)")]
        OCB = 970448,
    }
}
