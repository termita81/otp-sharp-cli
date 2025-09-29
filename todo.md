[ ] Pagination for codes
[ ] Allow the app to be navigated without any Enter key presses
[ ] Replace Thread.Sleep with tasks



Generate mock 52-long strings of alpha-digit chars, in JS - for testing some things in the app
code = (max) => {
    let res = '';
    const choices = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
    let i = 0;
    while (i++ < max)
        res += choices[Math.round(Math.random() * choices.length)];
    return res
}