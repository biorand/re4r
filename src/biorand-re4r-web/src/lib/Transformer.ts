export class Transformer {
    private transformRanges: number[][] = [];

    addTransformRange(start: number, end: number, transformStart: number, transformEnd: number) {
        this.transformRanges.push([start, end, transformStart, transformEnd]);
    }

    addStepRange(step: number, end: number) {
        if (this.transformRanges.length == 0) {
            this.transformRanges.push([0, Math.round(end / step), 0, end]);
        } else {
            const lastRange = this.transformRanges[this.transformRanges.length - 1];
            const beginInput = lastRange[1] + 1;
            const begin = lastRange[3] + step;
            const endInput = Math.round(beginInput + ((end - begin) / step));
            this.transformRanges.push([beginInput, endInput, begin, end]);
        }
    }

    transform(value: number): number {
        for (const range of this.transformRanges) {
            if (value >= range[0] && value <= range[1]) {
                const d = (value - range[0]) / (range[1] - range[0]);
                return this.lerp(d, range[2], range[3]);
            }
        }
        return 0;
    }

    untransform(value: number): number {
        for (const range of this.transformRanges) {
            if (value >= range[2] && value <= range[3]) {
                const d = (value - range[2]) / (range[3] - range[2]);
                return this.lerp(d, range[0], range[1]);
            }
        }
        return 0;
    }

    getMin() {
        if (this.transformRanges.length == 0) return 0;
        return this.transformRanges[0][0];
    }

    getMax() {
        if (this.transformRanges.length == 0) return 0;
        return this.transformRanges[this.transformRanges.length - 1][1];
    }

    private lerp(v: number, a: number, b: number): number {
        return Math.round(a + v * (b - a));
    }
}
