using System.Text.Json.Serialization;

namespace Applicate.Domain.Models;

public record ResourceDef(
    string Name,
    ResourceKind Kind,
    List<ResourceField> Fields,
    List<ResourceRelation> Relations,
    List<ResourceAction> Actions,
    QueryDefinition? Query
)
{
    public ResourceDef() : this("", ResourceKind.Table, [], [], [], null) { }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ResourceKind
{
    Table,
    Query,
    Report,
    Import,
    Log,
    Menu
}

public record ResourceField(
    string Name,
    string DataType, // f.eks. "date", "string", "decimal"
    bool IsNullable = false,
    string? Lookup = null, // Navnet på den Query, der leverer data (f.eks. "AllCustomers")
    List<string>? Options = null, // <--- NY: Til faste lister (Status)
    ValidationRules? Validation = null // <--- NY
);

public record ValidationRules(
    bool Required = false,
    double? Min = null,
    double? Max = null,
    int? MinLength = null,
    int? MaxLength = null,
    string? Regex = null
);

public record ResourceRelation(
    string Name,           // f.eks. "House"
    string TargetResource, // f.eks. "Booking"
    RelationType Type,
    Dictionary<string, string> FieldMapping
);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RelationType
{
    OneToOne,
    OneToMany,
    ManyToOne
}

public record ResourceAction(
    string Name,
    List<string> Roles,
    string? Description,
    List<ActionParameter> Parameters,
    List<WorkflowStep>? Steps // <--- NY: Listen af ting motoren skal gøre
);

public record WorkflowStep(
    string Type, // f.eks. "UpdateField", "Validate", "SendEmail"
    Dictionary<string, string> Args // Parametre til steppet
);

public record ActionParameter(
    string Name,
    string DataType
);

public record QueryDefinition(
    string Source,
    List<string> Select,
    List<string> Include, // <--- Liste af relationer at hente (f.eks. ["Customer"])
    List<QueryCondition> Conditions,
    List<QueryOrderBy> OrderBy
);

public record QueryCondition(
    string Field,
    string Op,
    string Value 
);

public record QueryOrderBy(
    string Field,
    string Direction
);