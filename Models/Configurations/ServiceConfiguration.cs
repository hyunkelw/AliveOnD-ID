namespace AliveOnD_ID.Models.Configurations;

public class ServiceConfiguration
{
    public AudioToTextConfig AudioToText { get; set; } = new();
    public LLMConfig LLM { get; set; } = new();
    public DIDConfig DID { get; set; } = new();
}
