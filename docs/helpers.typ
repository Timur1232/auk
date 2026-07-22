#set heading(numbering: "1.1")
#let section_head(body, numbering: true, outlined: true, offset_after: true) = {
  align(
    center,
    heading(
      numbering: if numbering { "1" } else { none },
      outlined: outlined,
      body
    )
  )
  if offset_after { v(2.5em) }
}

#set page(numbering: "1", number-align: top + center)

#let figi(path, caption, first: false, width: 70%) = {
  if first {
    V()
  }
  figure(
    caption: caption,
    image("./imgs/" + path, width: width),
  )
}

#let figure_caption_with_number_prefix(it, prefix: none) = {
  let fig-num = context counter("image").display()
  counter("image").step()
  let cap = if it.body != [] [ -- #it.body] else []
  let pref = if prefix != none [#prefix.] else []
  [Рисунок #pref#fig-num #cap]
}

#let figure_numbering(..nums, prefix: none) = {
  let res = nums.pos().map(str).join(".")
  if prefix != none {
    res = prefix + "." + res
  }
  res
}
