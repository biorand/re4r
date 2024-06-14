<script lang="ts">
    import ApexCharts from 'apexcharts';

    let className = '';
    export { className as class };
    export let options: any = undefined;

    export const chart = (node: any, options: any) => {
        let myChart = new ApexCharts(node, options);
        myChart.render();

        return {
            update(options: any) {
                myChart.updateOptions(options);
            },
            destroy() {
                myChart.destroy();
            }
        };
    };
</script>

<div class={className} use:chart={options} />
