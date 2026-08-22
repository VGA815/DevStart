namespace DevStart.Domain.StartupPartnerships
{
    /// <summary>
    /// What kind of arrangement the partnership is. Descriptive only — no kind is worth more than
    /// another, and the kind is deliberately *not* part of the dedup key: one partner is one record,
    /// however many things the two of you do together. Making it part of the key would open a cheap
    /// way to list the same partner three times and take the whole Berkus ceiling for it.
    ///
    /// Values are append-only — they are persisted in <c>startup_partnerships.kind</c>.
    /// </summary>
    public enum PartnershipKind
    {
        /// <summary>Пилот — the partner is running the product in a real setting.</summary>
        Pilot = 0,

        /// <summary>Клиент по договору — a signed commercial customer relationship.</summary>
        Customer = 1,

        /// <summary>Дистрибуция или реселлер — the partner sells or resells the product.</summary>
        Distribution = 2,

        /// <summary>Технологическая интеграция — the products are wired into each other.</summary>
        Integration = 3,

        /// <summary>Поставщик или подрядчик — the partner supplies something the product needs.</summary>
        Supplier = 4,

        /// <summary>НИОКР, вуз, лаборатория — joint research or development.</summary>
        Research = 5,

        /// <summary>Акселератор, фонд, институт развития — programme or grant support.</summary>
        Institutional = 6,

        /// <summary>Иное — described in the record's own text.</summary>
        Other = 7,
    }
}
