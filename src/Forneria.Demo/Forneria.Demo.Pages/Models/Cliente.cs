namespace Forneria.Demo.Pages;

public record Cliente
{
    public string Nome { get; set; } = "";
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public string? Cidade { get; set; }
    public string? Tipo { get; set; }
    public int Pedidos { get; set; }
    public string? Observacoes { get; set; }
}
