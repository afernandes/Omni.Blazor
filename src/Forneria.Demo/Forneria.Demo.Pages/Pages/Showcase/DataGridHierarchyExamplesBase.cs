using Omni.Blazor.Models;

namespace Forneria.Demo.Pages.Pages.Showcase;

public abstract class DataGridHierarchyExamplesBase : DataGridExamplesBase
{
    protected IReadOnlyList<SharingNode> SharingRoots { get; } = CreateSharingRoots();

    protected static bool InitiallyExpanded(SharingNode node) => true;

    protected static string? SharingRowClass(SharingNode node) => node.IsCurrent
        ? "omni-grid-row-highlight"
        : node.IsInconsistent ? "omni-grid-row-warning" : null;

    protected static int CountAll(IEnumerable<SharingNode> nodes)
    {
        var count = 0;
        foreach (var node in nodes)
        {
            count++;
            if (node.Children is not null)
                count += CountAll(node.Children);
        }

        return count;
    }

    protected static BadgeVariant LevelBadgeVariant(int? level) => level switch
    {
        1 => BadgeVariant.Info,
        2 => BadgeVariant.Plain,
        3 => BadgeVariant.Good,
        null => BadgeVariant.Danger,
        _ => BadgeVariant.Default
    };

    private static IReadOnlyList<SharingNode> CreateSharingRoots() =>
    [
        new SharingNode("OMNI Holding Brasil", "PROP-001", "0001", null, 1, false, false,
        [
            new SharingNode("Regional Sul", "PROP-001", "0010", "0001", 2, false, false,
            [
                new SharingNode("OMNI Large Enterprise Tecnologia", "PROP-001", "0100", "0010", 3, true, false),
                new SharingNode("OMNI Porto Alegre Centro", "PROP-001", "0101", "0010", 3, false, false),
                new SharingNode("OMNI Canoas", "PROP-001", "0102", "0010", 3, false, false),
            ]),
            new SharingNode("Regional Centro-Oeste", "PROP-001", "0020", "0001", 2, false, false,
            [
                new SharingNode("OMNI Curitiba", "PROP-001", "0200", "0020", 3, false, false),
                new SharingNode("OMNI Florianópolis", "PROP-001", "0201", "0020", 3, false, false),
            ]),
            new SharingNode("Regional Sudeste", "PROP-002", "0030", "0001", 2, false, false),
        ]),
        new SharingNode("Filial Órfã (pai não encontrado)", "PROP-003", "0999", "0099", null, false, true),
    ];

    protected sealed class SharingNode(
        string nome,
        string idProprietario,
        string idLoja,
        string? idPai,
        int? nivel,
        bool isCurrent,
        bool isInconsistent,
        List<SharingNode>? children = null)
    {
        public string Nome { get; } = nome;
        public string IdProprietario { get; } = idProprietario;
        public string IdLoja { get; } = idLoja;
        public string? IdPai { get; } = idPai;
        public int? Nivel { get; } = nivel;
        public bool IsCurrent { get; } = isCurrent;
        public bool IsInconsistent { get; } = isInconsistent;
        public List<SharingNode>? Children { get; } = children;

        public SharingNode WithChildren(List<SharingNode>? nextChildren) => new(
            Nome,
            IdProprietario,
            IdLoja,
            IdPai,
            Nivel,
            IsCurrent,
            IsInconsistent,
            nextChildren);
    }
}
