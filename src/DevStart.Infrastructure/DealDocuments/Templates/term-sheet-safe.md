# Term Sheet — SAFE

**Startup:** {{startup_name}}
**Stage:** {{startup_stage}}
**Deal id:** `{{deal_id}}`
**Application id:** `{{application_id}}`
**Generated at:** {{generated_at}}

---

## Investment

- **Instrument:** SAFE (Simple Agreement for Future Equity)
- **Amount:** ₽{{amount}}
- **Valuation cap:** ₽{{valuation_cap}}
- **Discount:** {{discount_pct}} %
- **Liquidation preference:** {{liquidation_preference}}x
- **Pro-rata rights:** {{pro_rata_rights}}

> Conversion math: investor's effective price will be `min(valuation_cap, next_round_price × (1 − discount))`.
> Settlement and shareholder rights are governed by the executed SAFE agreement.

## Calculated investor share

- **Investor share after deal:** {{investor_share_pct}} %
- **Founders' combined share after deal:** {{founders_total_after_pct}} %

> *Calculated assuming conversion at the stated cap. Actual conversion math depends on the next priced round.*

## Cap table after deal

{{cap_table_md_table}}

## Founders & vesting

{{founders_breakdown}}

## Standard terms (not negotiable in this template)

- **Anti-dilution:** Broad-Based Weighted Average (BBWA)
- **Founder vesting:** per the Founders & vesting section above (default: 4 years with a 1-year cliff)
- **Pro-rata rights:** as marked above
- **Exclusivity:** 30 days, legally binding
- **Confidentiality:** standard

## Platform score & indicative range (informational)

- **Total score:** {{score_total}} / 100
- Team {{score_team}} · Market {{score_market}} · Product {{score_product}} · Traction {{score_traction}} · Competition {{score_competition}}
- **Расчётный ориентир диапазона стоимости (информационно):** ₽{{valuation_low}} – ₽{{valuation_high}}
- **Methods used:** {{methods_used}}
- **Methodology version:** {{methodology_version}}
- **Calculated at:** {{calculated_at}}

> **Дисклеймер.** Указанный диапазон носит информационно-аналитический характер, рассчитан
> алгоритмически по данным платформы и **не является отчётом об оценке** в значении Федерального
> закона № 135-ФЗ «Об оценочной деятельности в Российской Федерации», а также **не является
> индивидуальной инвестиционной рекомендацией** в значении Федерального закона № 39-ФЗ «О рынке
> ценных бумаг». Итоговые условия сделки определяются сторонами самостоятельно.

## Warnings

{{flags_section}}
