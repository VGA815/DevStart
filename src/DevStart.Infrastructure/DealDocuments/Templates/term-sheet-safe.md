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

## Standard terms (not negotiable in this template)

- **Anti-dilution:** Broad-Based Weighted Average (BBWA)
- **Founder vesting:** 4 years with 1 year cliff
- **Pro-rata rights:** as marked above
- **Exclusivity:** 30 days, legally binding
- **Confidentiality:** standard

## Platform score & valuation reference

- **Total score:** {{score_total}} / 100
- Team {{score_team}} · Market {{score_market}} · Product {{score_product}} · Traction {{score_traction}} · Competition {{score_competition}}
- **Valuation range (platform estimate):** ₽{{valuation_low}} – ₽{{valuation_high}}
- **Methods used:** {{methods_used}}
- **Methodology version:** {{methodology_version}}
- **Calculated at:** {{calculated_at}}

## Warnings

{{flags_section}}
